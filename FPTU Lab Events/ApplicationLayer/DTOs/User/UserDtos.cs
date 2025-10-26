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
  /// <summary>
  /// Email của người dùng (phải unique)
  /// </summary>
  public string Email { get; set; } = null!;
  
  /// <summary>
  /// Tên đăng nhập (phải unique)
  /// </summary>
  public string Username { get; set; } = null!;
  
  /// <summary>
  /// Mật khẩu (sẽ được hash trước khi lưu)
  /// </summary>
  public string Password { get; set; } = null!;
  
  /// <summary>
  /// Họ và tên đầy đủ
  /// </summary>
  public string Fullname { get; set; } = null!;
  
  /// <summary>
  /// Mã số sinh viên (tùy chọn)
  /// </summary>
  public string? MSSV { get; set; }
  
  /// <summary>
  /// Danh sách roles (tùy chọn, mặc định là Student)
  /// </summary>
  public string[]? Roles { get; set; }
}

public class UpdateUserRequest
{
  /// <summary>
  /// Họ và tên mới (tùy chọn)
  /// </summary>
  public string? Fullname { get; set; }
  
  /// <summary>
  /// Mã số sinh viên mới (tùy chọn)
  /// </summary>
  public string? MSSV { get; set; }
  
  /// <summary>
  /// Danh sách roles mới (tùy chọn)
  /// </summary>
  public string[]? Roles { get; set; }
}

public class UpdateStatusRequest
{
  /// <summary>
  /// Trạng thái mới của người dùng (Active=0, Inactive=1, Locked=2)
  /// </summary>
  public string Status { get; set; } = null!;
}

public class UpdateUserRolesRequest
{
  public string[] Roles { get; set; } = Array.Empty<string>();
}


