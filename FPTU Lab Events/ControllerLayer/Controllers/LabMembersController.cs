using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.User;
using Application.ResponseCode;
using Application.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Hiền
    /// </summary>
    [ApiController]
    [Route("api/labs/{labId:guid}/members")]
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
        public async Task<IActionResult> Get(Guid labId)
        {
            try
            {
                var result = await _service.GetByLabAsync(labId);
                return SuccessResp.Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Thêm thành viên vào Lab.
        /// </summary>
        /// <param name="labId">Id của Lab.</param>
        /// <param name="request">Thông tin tạo thành viên.</param>
        /// <returns>Thành viên vừa thêm.</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add(Guid labId, CreateLabMemberRequest request)
        {
            try
            {
                request.LabId = labId;
                var result = await _service.AddAsync(request);
                return SuccessResp.Created(result);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
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
        public async Task<IActionResult> Update(Guid labId, Guid id, UpdateLabMemberRequest request)
        {
            try
            {
                var result = await _service.UpdateAsync(id, request);
                return SuccessResp.Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
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
            try
            {
                await _service.RemoveAsync(id);
                return SuccessResp.NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
    }
}


