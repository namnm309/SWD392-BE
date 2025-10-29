using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Application.DTOs.Auth;
using DomainLayer.Constants;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Core.JWT;
using InfrastructureLayer.Core.Mail;
using InfrastructureLayer.Core.Redis;
using InfrastructureLayer.Data;
using InfrastructureLayer.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Auth;

public interface IAuthService
{
  Task<TokenResponse> RegisterAsync(RegisterRequest request);
  Task<TokenResponse> LoginAsync(LoginRequest request);
  Task<TokenResponse> RefreshAsync(string refreshToken, string? device, string? ipAddress);
  Task LogoutAsync(Guid sessionId);
  Task<object> GetMeAsync(Guid userId);
  Task<string> GetGoogleAuthorizationUrlAsync(string redirectUri, string state);
  Task<TokenResponse> HandleGoogleCallbackAsync(string code, string redirectUri, string[] allowedDomains);
  Task<TokenResponse> LoginWithGoogleIdTokenAsync(string idToken, string[] allowedDomains);
  Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
  Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
}

public class AuthService : IAuthService
{
  private readonly LabDbContext _db;
  private readonly IJwtService _jwt;
  private readonly IConfiguration _config;
  private readonly IMailService _mailService;
  private readonly IRedisService _redisService;

  public AuthService(LabDbContext db, IJwtService jwt, IConfiguration config, IMailService mailService, IRedisService redisService)
  {
    _db = db;
    _jwt = jwt;
    _config = config;
    _mailService = mailService;
    _redisService = redisService;
  }

  public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var username = request.Username.Trim();

    // Chỉ cho phép đăng ký bằng email FPT
    var domain = email.Split('@').LastOrDefault() ?? string.Empty;
    if (!domain.EndsWith("fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
    {
      throw new Exception("Chỉ cho phép mail fpt.edu.vn");
    }

    if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
      throw new Exception("Email đã tồn tại");
    if (await _db.Users.AnyAsync(u => u.Username == username))
      throw new Exception("Username đã tồn tại");

    var roleStudent = await _db.Roles.FirstAsync(r => r.name == "Student");

    var user = new Users
    {
      Id = Guid.NewGuid(),
      Email = email,
      Username = username,
      Fullname = request.Fullname ?? username,
      Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
      MSSV = string.IsNullOrWhiteSpace(request.MSSV) ? ExtractMssvFromFptEmail(email) : request.MSSV,
      status = UserStatus.Active,
      CreatedAt = DateTime.UtcNow,
      LastUpdatedAt = DateTime.UtcNow
    };
    user.Roles.Add(roleStudent);

    _db.Users.Add(user);

    var (session, refreshPlain1) = CreateSession(user, device: null, ipAddress: null);

    await _db.SaveChangesAsync();

    return BuildTokens(user, session, refreshPlain1);
  }

  public async Task<TokenResponse> LoginAsync(LoginRequest request)
  {
    var identifier = request.Identifier.Trim();
    var user = await _db.Users
      .Include(u => u.Roles)
      .FirstOrDefaultAsync(u => u.Username == identifier || u.Email.ToLower() == identifier.ToLower());

    if (user == null) throw new Exception("Invalid credentials");
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password)) throw new Exception("Invalid credentials");
    if (user.status != UserStatus.Active) throw new Exception("Account not active");

    var (session2, refreshPlain2) = CreateSession(user, device: null, ipAddress: null);
    await _db.SaveChangesAsync();
    return BuildTokens(user, session2, refreshPlain2);
  }

  public async Task<TokenResponse> RefreshAsync(string refreshToken, string? device, string? ipAddress)
  {
    // Find session by refresh token hash
    var tokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
    var now = DateTime.UtcNow;

    var candidates = await _db.UserSessions
      .Include(s => s.User)
      .ThenInclude(u => u.Roles)
      .Where(s => s.RevokedAt == null && s.ExpiresAt > now)
      .ToListAsync();
    var session = candidates.FirstOrDefault(s => BCrypt.Net.BCrypt.Verify(refreshToken, s.RefreshTokenHash));

    if (session == null) throw new Exception("Invalid refresh token");

    // rotate
    session.RevokedAt = now;
    _db.UserSessions.Update(session);

    var (newSession, refreshPlain) = CreateSession(session.User, device, ipAddress);
    await _db.SaveChangesAsync();
    return BuildTokens(session.User, newSession, refreshPlain);
  }

  public async Task LogoutAsync(Guid sessionId)
  {
    var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
    if (session != null)
    {
      session.RevokedAt = DateTime.UtcNow;
      _db.UserSessions.Update(session);
      await _db.SaveChangesAsync();
    }
  }

  public async Task<object> GetMeAsync(Guid userId)
  {
    var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) throw new Exception("Not found");
    return new
    {
      id = user.Id,
      email = user.Email,
      username = user.Username,
      fullname = user.Fullname,
      roles = user.Roles.Select(r => r.name).ToArray(),
      status = user.status.ToString()
    };
  }

  public Task<string> GetGoogleAuthorizationUrlAsync(string redirectUri, string state)
  {
    var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
                 ?? _config["Google:ClientId"];
    var scopes = new[] { "openid", "email", "profile" };
    var scopeParam = string.Join(" ", scopes);

    var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={Uri.EscapeDataString(clientId!)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopeParam)}&access_type=offline&prompt=consent&state={Uri.EscapeDataString(state)}";
    return Task.FromResult(url);
  }

  public async Task<TokenResponse> HandleGoogleCallbackAsync(string code, string redirectUri, string[] allowedDomains)
  {
    var clientId = (Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
                    ?? _config["Google:ClientId"])!;
    var clientSecret = (Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
                        ?? _config["Google:ClientSecret"])!;

    using var http = new System.Net.Http.HttpClient();
    var tokenResp = await http.PostAsync("https://oauth2.googleapis.com/token", new System.Net.Http.FormUrlEncodedContent(new[]
    {
      new System.Collections.Generic.KeyValuePair<string,string>("code", code),
      new System.Collections.Generic.KeyValuePair<string,string>("client_id", clientId),
      new System.Collections.Generic.KeyValuePair<string,string>("client_secret", clientSecret),
      new System.Collections.Generic.KeyValuePair<string,string>("redirect_uri", redirectUri),
      new System.Collections.Generic.KeyValuePair<string,string>("grant_type", "authorization_code"),
    }));
    tokenResp.EnsureSuccessStatusCode();
    var payload = JsonDocument.Parse(await tokenResp.Content.ReadAsStringAsync());
    var idToken = payload.RootElement.GetProperty("id_token").GetString();

    var googlePayload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken);
    var email = googlePayload.Email.ToLowerInvariant();
    var domain = googlePayload.HostedDomain ?? email.Split('@').Last();
    if (!allowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
      throw new Exception("Email domain not allowed");

    var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email.ToLower() == email);
    if (user == null)
    {
      var roleName = DetermineRoleFromEmail(email);
      var roleToAssign = await _db.Roles.FirstAsync(r => r.name == roleName);
      var initialPlainPassword = GenerateReadablePassword(12);
      user = new Users
      {
        Id = Guid.NewGuid(),
        Email = email,
        Username = email.Split('@')[0],
        Fullname = googlePayload.Name ?? email,
        Password = BCrypt.Net.BCrypt.HashPassword(initialPlainPassword),
        MSSV = ExtractMssvFromFptEmail(email),
        status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        LastUpdatedAt = DateTime.UtcNow,
      };
      user.Roles.Add(roleToAssign);
      _db.Users.Add(user);

      await TrySendInitialPasswordEmailAsync(email, user.Username, initialPlainPassword);
    }
    else
    {
      // Tự động gán MSSV nếu chưa có
      if (string.IsNullOrWhiteSpace(user.MSSV))
      {
        var extracted = ExtractMssvFromFptEmail(email);
        if (!string.IsNullOrWhiteSpace(extracted))
        {
          user.MSSV = extracted;
          user.LastUpdatedAt = DateTime.UtcNow;
          _db.Users.Update(user);
        }
      }
    }

    var (session, refreshPlain) = CreateSession(user, device: "google-oauth", ipAddress: null);
    await _db.SaveChangesAsync();
    return BuildTokens(user, session, refreshPlain);
  }

  public async Task<TokenResponse> LoginWithGoogleIdTokenAsync(string idToken, string[] allowedDomains)
  {
    var googlePayload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken);
    var email = googlePayload.Email.ToLowerInvariant();
    var domain = googlePayload.HostedDomain ?? email.Split('@').Last();
    if (!allowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
      throw new Exception("Email domain not allowed");

    var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email.ToLower() == email);
    if (user == null)
    {
      var roleName = DetermineRoleFromEmail(email);
      var roleToAssign = await _db.Roles.FirstAsync(r => r.name == roleName);
      var initialPlainPassword = GenerateReadablePassword(12);
      user = new Users
      {
        Id = Guid.NewGuid(),
        Email = email,
        Username = email.Split('@')[0],
        Fullname = googlePayload.Name ?? email,
        Password = BCrypt.Net.BCrypt.HashPassword(initialPlainPassword),
        MSSV = ExtractMssvFromFptEmail(email),
        status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        LastUpdatedAt = DateTime.UtcNow,
      };
      user.Roles.Add(roleToAssign);
      _db.Users.Add(user);

      await TrySendInitialPasswordEmailAsync(email, user.Username, initialPlainPassword);
    }
    else
    {
      // Tự động gán MSSV nếu chưa có
      if (string.IsNullOrWhiteSpace(user.MSSV))
      {
        var extracted = ExtractMssvFromFptEmail(email);
        if (!string.IsNullOrWhiteSpace(extracted))
        {
          user.MSSV = extracted;
          user.LastUpdatedAt = DateTime.UtcNow;
          _db.Users.Update(user);
        }
      }
    }

    var (session, refreshPlain) = CreateSession(user, device: "google-idtoken", ipAddress: null);
    await _db.SaveChangesAsync();
    return BuildTokens(user, session, refreshPlain);
  }

  private (UserSession session, string refreshPlain) CreateSession(Users user, string? device, string? ipAddress)
  {
    var refreshPlain = GenerateRandomToken(DomainLayer.Constants.JwtConst.REFRESH_TOKEN_LENGTH);
    var refreshHash = BCrypt.Net.BCrypt.HashPassword(refreshPlain);
    var session = new UserSession
    {
      Id = Guid.NewGuid(),
      UserId = user.Id,
      RefreshTokenHash = refreshHash,
      ExpiresAt = DateTime.UtcNow.AddSeconds(DomainLayer.Constants.JwtConst.REFRESH_TOKEN_EXP),
      Device = device,
      IpAddress = ipAddress,
      CreatedAt = DateTime.UtcNow,
      LastUpdatedAt = DateTime.UtcNow
    };
    _db.UserSessions.Add(session);
    return (session, refreshPlain);
  }

  private TokenResponse BuildTokens(Users user, UserSession session, string refreshPlain)
  {
    var primaryRole = user.Roles.Select(r => r.name).FirstOrDefault() ?? "Student";
    var accessToken = _jwt.GenerateToken(user.Id, primaryRole, session.Id, user.Email, user.status, DomainLayer.Constants.JwtConst.ACCESS_TOKEN_EXP);

    return new TokenResponse
    {
      AccessToken = accessToken,
      RefreshToken = refreshPlain,
      ExpiresIn = DomainLayer.Constants.JwtConst.ACCESS_TOKEN_EXP,
      User = new
      {
        id = user.Id,
        email = user.Email,
        username = user.Username,
        fullname = user.Fullname,
        mssv = user.MSSV,
        roles = user.Roles.Select(r => r.name).ToArray(),
        status = user.status.ToString()
      }
    };
  }

  //regex
  private static string? ExtractMssvFromFptEmail(string email)
  {
    if (string.IsNullOrWhiteSpace(email)) return null;
    var parts = email.Split('@');
    if (parts.Length != 2) return null;
    var domain = parts[1].ToLowerInvariant();
    if (!domain.EndsWith("fpt.edu.vn")) return null;

    var local = parts[0].ToLowerInvariant();
    // 2 chữ trước số là mã ngành 
    var match = Regex.Match(local, "([a-z]{2})(\\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success) return null;
    var letters = match.Groups[1].Value.ToUpperInvariant();
    var digits = match.Groups[2].Value;
    return string.IsNullOrEmpty(letters) || string.IsNullOrEmpty(digits) ? null : letters + digits;
  }

  private static string DetermineRoleFromEmail(string email)
  {
    // Mặc định: Student nếu local-part kết thúc bằng 2 chữ + 6 số, ngược lại: Lecturer
    if (string.IsNullOrWhiteSpace(email)) return "Lecturer";
    var parts = email.Split('@');
    if (parts.Length != 2) return "Lecturer";
    var local = parts[0].ToLowerInvariant();
    var match = Regex.Match(local, "([a-z]{2})(\\d{6})$", RegexOptions.IgnoreCase);
    return match.Success ? "Student" : "Lecturer";
  }

  private static string GenerateRandomToken(int length)
  {
    var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
    var bytes = new byte[length];
    rng.GetBytes(bytes);
    return Convert.ToBase64String(bytes);
  }

  private static string GenerateReadablePassword(int length)
  {
    const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string lower = "abcdefghijkmnopqrstuvwxyz";
    const string digits = "23456789";
    const string specials = "@#$%&*?";
    var all = upper + lower + digits + specials;

    var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
    var buffer = new byte[length];
    var chars = new char[length];
    rng.GetBytes(buffer);
    for (var i = 0; i < length; i++)
    {
      var idx = buffer[i] % all.Length;
      chars[i] = all[idx];
    }
    return new string(chars);
  }

  private async Task TrySendInitialPasswordEmailAsync(string email, string username, string plainPassword)
  {
    try
    {
      var subject = "FPTU Lab Events - Mật khẩu lần đầu";
      var message = $@"<p>Xin chào {System.Net.WebUtility.HtmlEncode(username)},</p>
                    <p>Tài khoản của bạn đã được tạo khi đăng nhập bằng email FPT.</p>
                    <p><b>Tên đăng nhập:</b> {System.Net.WebUtility.HtmlEncode(username)}<br/>
                    <b>Mật khẩu tạm thời:</b> {System.Net.WebUtility.HtmlEncode(plainPassword)}</p>
                    <p>Vì lý do bảo mật, hãy đăng nhập và đổi mật khẩu ngay sau lần đầu sử dụng.</p>
                    <p>Trân trọng,<br/>FPTU Lab Events</p>";
      await _mailService.SendEmailAsync(email, subject, message);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[MailError] Failed to send initial password to {email}: {ex.Message}");
    }
  }

  public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    
    // Kiểm tra user có tồn tại không
    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
    if (user == null)
    {
      // Không tiết lộ thông tin user có tồn tại hay không
      return new ForgotPasswordResponse 
      { 
        Message = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để reset mật khẩu." 
      };
    }

    // Generate OTP 6 số
    var otp = GenerateOtp(6);
    
    // Lưu OTP vào Redis với thời gian hết hạn 5 phút
    await _redisService.SetOtpAsync(email, otp, TimeSpan.FromMinutes(5));
    
    // Gửi email OTP
    await TrySendOtpEmailAsync(email, user.Username, otp);
    
    return new ForgotPasswordResponse 
    { 
      Message = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để reset mật khẩu." 
    };
  }

  public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var otp = request.Otp.Trim();
    var newPassword = request.NewPassword.Trim();
    
    // Validate OTP
    var isValidOtp = await _redisService.ValidateOtpAsync(email, otp);
    if (!isValidOtp)
    {
      throw new Exception("Mã OTP không hợp lệ hoặc đã hết hạn");
    }
    
    // Tìm user
    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
    if (user == null)
    {
      throw new Exception("Người dùng không tồn tại");
    }
    
    // Cập nhật mật khẩu mới
    user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
    user.LastUpdatedAt = DateTime.UtcNow;
    
    _db.Users.Update(user);
    await _db.SaveChangesAsync();
    
    return new ResetPasswordResponse 
    { 
      Message = "Mật khẩu đã được reset thành công. Bạn có thể đăng nhập với mật khẩu mới." 
    };
  }

  private static string GenerateOtp(int length)
  {
    var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
    var bytes = new byte[length];
    rng.GetBytes(bytes);
    
    var otp = "";
    for (int i = 0; i < length; i++)
    {
      otp += (bytes[i] % 10).ToString();
    }
    return otp;
  }

  private async Task TrySendOtpEmailAsync(string email, string username, string otp)
  {
    try
    {
      var subject = "FPTU Lab Events - Mã OTP Reset Mật Khẩu";
      var message = $@"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background-color: #f8f9fa; padding: 30px; border-radius: 10px; text-align: center;"">
          <h2 style=""color: #333; margin-bottom: 20px;"">Reset Mật Khẩu</h2>
          <p style=""color: #666; font-size: 16px; margin-bottom: 20px;"">Xin chào <strong>{System.Net.WebUtility.HtmlEncode(username)}</strong>,</p>
          <p style=""color: #666; font-size: 16px; margin-bottom: 30px;"">Bạn đã yêu cầu reset mật khẩu cho tài khoản FPTU Lab Events.</p>
          
          <div style=""background-color: #fff; border: 2px dashed #007bff; padding: 20px; border-radius: 8px; margin: 20px 0;"">
            <p style=""color: #333; font-size: 14px; margin: 0 0 10px 0;"">Mã OTP của bạn:</p>
            <h1 style=""color: #007bff; font-size: 32px; font-weight: bold; margin: 0; letter-spacing: 5px;"">{otp}</h1>
          </div>
          
          <p style=""color: #e74c3c; font-size: 14px; margin: 20px 0;""><strong>⚠️ Lưu ý:</strong></p>
          <ul style=""color: #666; font-size: 14px; text-align: left; margin: 20px 0;"">
            <li>Mã OTP có hiệu lực trong <strong>5 phút</strong></li>
            <li>Chỉ sử dụng một lần duy nhất</li>
            <li>Không chia sẻ mã này với bất kỳ ai</li>
          </ul>
          
          <p style=""color: #666; font-size: 14px; margin-top: 30px;"">Nếu bạn không yêu cầu reset mật khẩu, vui lòng bỏ qua email này.</p>
          
          <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
          <p style=""color: #999; font-size: 12px;"">Trân trọng,<br/>Đội ngũ FPTU Lab Events</p>
        </div>
      </div>";
      
      await _mailService.SendEmailAsync(email, subject, message);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[MailError] Failed to send OTP to {email}: {ex.Message}");
      throw new Exception("Không thể gửi email OTP. Vui lòng thử lại sau.");
    }
  }
}


