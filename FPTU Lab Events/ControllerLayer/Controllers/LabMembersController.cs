using Application.DTOs.User;
using Application.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Nam
    /// </summary>
    [ApiController]
	[Route("api/labs/{labId:guid}/members")]
	/// <summary>
	/// Quản lý thành viên của Lab: liệt kê, thêm, cập nhật, xóa.
	/// </summary>
	public class LabMembersController : ControllerBase
	{
		private readonly ILabMemberService _service;

		public LabMembersController(ILabMemberService service)
		{
			_service = service;
		}

		/// <summary>
		/// Lấy danh sách thành viên theo Lab.
		/// </summary>
		/// <param name="labId">Id của Lab.</param>
		/// <returns>Danh sách thành viên.</returns>
		[HttpGet]
		public async Task<ActionResult<IReadOnlyList<LabMemberListItem>>> Get(Guid labId)
		{
			var result = await _service.GetByLabAsync(labId);
			return Ok(result);
		}

		/// <summary>
		/// Thêm thành viên vào Lab.
		/// </summary>
		/// <param name="labId">Id của Lab.</param>
		/// <param name="request">Thông tin tạo thành viên.</param>
		/// <returns>Thành viên vừa thêm.</returns>
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<LabMemberDetail>> Add(Guid labId, CreateLabMemberRequest request)
		{
			request.LabId = labId;
			var result = await _service.AddAsync(request);
			return Ok(result);
		}

		/// <summary>
		/// Cập nhật vai trò/trạng thái thành viên Lab.
		/// </summary>
		/// <param name="labId">Id của Lab.</param>
		/// <param name="id">Id thành viên.</param>
		/// <param name="request">Thông tin cập nhật.</param>
		/// <returns>Thành viên sau cập nhật.</returns>
		[HttpPatch("{id}")]
		[Authorize]
		public async Task<ActionResult<LabMemberDetail>> Update(Guid labId, Guid id, UpdateLabMemberRequest request)
		{
			var result = await _service.UpdateAsync(id, request);
			return Ok(result);
		}

		/// <summary>
		/// Xóa một thành viên khỏi Lab.
		/// </summary>
		/// <param name="labId">Id của Lab.</param>
		/// <param name="id">Id thành viên.</param>
		/// <returns>No content.</returns>
		[HttpDelete("{id}")]
		[Authorize]
		public async Task<IActionResult> Remove(Guid labId, Guid id)
		{
			await _service.RemoveAsync(id);
			return NoContent();
		}
	}
}


