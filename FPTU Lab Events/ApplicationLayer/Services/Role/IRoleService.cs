using Application.DTOs.Role;

namespace Application.Services.Role;

public interface IRoleService
{
    Task<IReadOnlyList<RoleListItem>> ListAsync(RoleFilterRequest? filter = null);
    Task<RoleDetail> GetByIdAsync(Guid id);
    Task<RoleDetail> CreateAsync(CreateRoleRequest request);
    Task<RoleDetail> UpdateAsync(Guid id, UpdateRoleRequest request);
    Task DeleteAsync(Guid id);
}


