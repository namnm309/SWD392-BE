using Application.DTOs.User;
using DomainLayer.Entities;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.User
{
	public class LabMemberService : ILabMemberService
	{
		private readonly LabDbContext _db;

		public LabMemberService(LabDbContext db)
		{
			_db = db;
		}

		public async Task<IReadOnlyList<LabMemberListItem>> GetByLabAsync(Guid labId)
		{
			var members = await _db.LabMembers
				.Include(m => m.User)
				.Include(m => m.Lab)
				.Where(m => m.LabId == labId)
				.OrderByDescending(m => m.JoinedAt)
				.ToListAsync();

			return members.Select(m => new LabMemberListItem
			{
				Id = m.Id,
				LabId = m.LabId,
				LabName = m.Lab.Name,
				UserId = m.UserId,
				Fullname = m.User.Fullname,
				Role = m.Role,
				Status = m.Status,
				JoinedAt = m.JoinedAt
			}).ToList();
		}

		public async Task<LabMemberDetail> AddAsync(CreateLabMemberRequest request)
		{
			// Unique (LabId, UserId)
			var exists = await _db.LabMembers.AnyAsync(m => m.LabId == request.LabId && m.UserId == request.UserId);
			if (exists) throw new Exception("User already member of this lab");

			var member = new LabMember
			{
				Id = Guid.NewGuid(),
				LabId = request.LabId,
				UserId = request.UserId,
				Role = request.Role,
				Status = LabMemberStatus.Active,
				JoinedAt = DateTime.UtcNow,
				CreatedAt = DateTime.UtcNow,
				LastUpdatedAt = DateTime.UtcNow
			};

			_db.LabMembers.Add(member);
			await _db.SaveChangesAsync();

			return await GetDetail(member.Id);
		}

		public async Task<LabMemberDetail> UpdateAsync(Guid id, UpdateLabMemberRequest request)
		{
			var member = await _db.LabMembers.Include(m => m.User).Include(m => m.Lab).FirstOrDefaultAsync(m => m.Id == id)
				?? throw new Exception("Lab member not found");

			if (request.Role.HasValue) member.Role = request.Role.Value;
			if (request.Status.HasValue) member.Status = request.Status.Value;
			if (request.LeftAt.HasValue) member.LeftAt = request.LeftAt.Value;
			member.LastUpdatedAt = DateTime.UtcNow;

			_db.LabMembers.Update(member);
			await _db.SaveChangesAsync();

			return await GetDetail(member.Id);
		}

		public async Task RemoveAsync(Guid id)
		{
			var member = await _db.LabMembers.FirstOrDefaultAsync(m => m.Id == id)
				?? throw new Exception("Lab member not found");
			_db.LabMembers.Remove(member);
			await _db.SaveChangesAsync();
		}

		private async Task<LabMemberDetail> GetDetail(Guid id)
		{
			var m = await _db.LabMembers.Include(x => x.User).Include(x => x.Lab).FirstAsync(x => x.Id == id);
			return new LabMemberDetail
			{
				Id = m.Id,
				LabId = m.LabId,
				LabName = m.Lab.Name,
				UserId = m.UserId,
				Fullname = m.User.Fullname,
				Role = m.Role,
				Status = m.Status,
				JoinedAt = m.JoinedAt,
				LeftAt = m.LeftAt
			};
		}
	}
}


