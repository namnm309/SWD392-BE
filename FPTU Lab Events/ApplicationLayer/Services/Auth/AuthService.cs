using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Auth;
using DomainLayer.Constants;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Core.JWT;
using InfrastructureLayer.Core.Mail;
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
}

public class AuthService : IAuthService
{
  private readonly LabDbContext _db;
  private readonly IJwtService _jwt;
  private readonly IConfiguration _config;
  private readonly IMailService _mailService;

  public AuthService(LabDbContext db, IJwtService jwt, IConfiguration config, IMailService mailService)
  {
    _db = db;
    _jwt = jwt;
    _config = config;
    _mailService = mailService;
  }

  public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var username = request.Username.Trim();

    if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
      throw new Exception("Email already exists");
    if (await _db.Users.AnyAsync(u => u.Username == username))
      throw new Exception("Username already exists");

    var roleStudent = await _db.Roles.FirstAsync(r => r.name == "Student");

    var user = new Users
    {
      Id = Guid.NewGuid(),
      Email = email,
      Username = username,
      Fullname = request.Fullname ?? username,
      Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
      MSSV = request.MSSV,
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
      var roleStudent = await _db.Roles.FirstAsync(r => r.name == "Student");
      var initialPlainPassword = GenerateReadablePassword(12);
      user = new Users
      {
        Id = Guid.NewGuid(),
        Email = email,
        Username = email.Split('@')[0],
        Fullname = googlePayload.Name ?? email,
        Password = BCrypt.Net.BCrypt.HashPassword(initialPlainPassword),
        status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        LastUpdatedAt = DateTime.UtcNow,
      };
      user.Roles.Add(roleStudent);
      _db.Users.Add(user);

      await TrySendInitialPasswordEmailAsync(email, user.Username, initialPlainPassword);
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
      var roleStudent = await _db.Roles.FirstAsync(r => r.name == "Student");
      var initialPlainPassword = GenerateReadablePassword(12);
      user = new Users
      {
        Id = Guid.NewGuid(),
        Email = email,
        Username = email.Split('@')[0],
        Fullname = googlePayload.Name ?? email,
        Password = BCrypt.Net.BCrypt.HashPassword(initialPlainPassword),
        status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        LastUpdatedAt = DateTime.UtcNow,
      };
      user.Roles.Add(roleStudent);
      _db.Users.Add(user);

      await TrySendInitialPasswordEmailAsync(email, user.Username, initialPlainPassword);
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
        roles = user.Roles.Select(r => r.name).ToArray(),
        status = user.status.ToString()
      }
    };
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
}


