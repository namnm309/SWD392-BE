using System.Threading.Tasks;
using Application.DTOs.User;

namespace Application.Services.User;

public interface IUserService
{
  Task<IReadOnlyList<UserListItem>> ListAsync(int? page = null, int? pageSize = null);
  Task<UserDetail> GetByIdAsync(Guid id);
  Task<UserDetail> CreateAsync(CreateUserRequest request);
  Task<UserDetail> UpdateAsync(Guid id, UpdateUserRequest request);
  Task DeleteAsync(Guid id);
  Task<UserDetail> UpdateStatusAsync(Guid id, UpdateStatusRequest request);
}


