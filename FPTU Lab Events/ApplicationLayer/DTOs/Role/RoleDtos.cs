using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Role;

public class RoleListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}

public class RoleDetail
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int UsersCount { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = null!;

    [Required]
    [MinLength(2)]
    public string Description { get; set; } = null!;
}

public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class RoleFilterRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}


