namespace Application.DTOs.User;

public class UserListItem
{
  public Guid Id { get; set; }
  public string Email { get; set; } = null!;
  public string Username { get; set; } = null!;
  public string Fullname { get; set; } = null!;
  public string[] Roles { get; set; } = Array.Empty<string>();
  public string Status { get; set; } = null!;
}

public class UserDetail : UserListItem
{
  public string? MSSV { get; set; }
}

public class CreateUserRequest
{
  public string Email { get; set; } = null!;
  public string Username { get; set; } = null!;
  public string Password { get; set; } = null!;
  public string Fullname { get; set; } = null!;
  public string? MSSV { get; set; }
  public string[]? Roles { get; set; }
}

public class UpdateUserRequest
{
  public string? Fullname { get; set; }
  public string? MSSV { get; set; }
  public string[]? Roles { get; set; }
}

public class UpdateStatusRequest
{
  public string Status { get; set; } = null!; // Active/Inactive/Locked
}


