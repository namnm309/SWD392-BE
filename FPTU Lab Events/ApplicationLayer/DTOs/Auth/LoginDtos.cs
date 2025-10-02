namespace Application.DTOs.Auth;

public class RegisterRequest
{
  public string Email { get; set; } = null!;
  public string Username { get; set; } = null!;
  public string Password { get; set; } = null!;
  public string? Fullname { get; set; }
  public string? MSSV { get; set; }
}

public class LoginRequest
{
  public string Identifier { get; set; } = null!; // username or email
  public string Password { get; set; } = null!;
}

public class TokenResponse
{
  public string AccessToken { get; set; } = null!;
  public string RefreshToken { get; set; } = null!;
  public int ExpiresIn { get; set; }
  public object User { get; set; } = null!;
}

public class RefreshRequest
{
  public string RefreshToken { get; set; } = null!;
}

public class GoogleAuthStartResponse
{
  public string AuthorizationUrl { get; set; } = null!;
}

public class GoogleCallbackRequest
{
  public string Code { get; set; } = null!;
  public string RedirectUri { get; set; } = null!;
}


