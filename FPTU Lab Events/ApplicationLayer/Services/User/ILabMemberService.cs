using Application.DTOs.User;

namespace Application.Services.User
{
	public interface ILabMemberService
	{
		Task<IReadOnlyList<LabMemberListItem>> GetByLabAsync(Guid labId);
		Task<LabMemberDetail> AddAsync(CreateLabMemberRequest request);
		Task<LabMemberDetail> UpdateAsync(Guid id, UpdateLabMemberRequest request);
		Task RemoveAsync(Guid id);
	}
}


