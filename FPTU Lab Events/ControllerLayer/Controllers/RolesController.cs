using Application.DTOs.Role;
using Application.ResponseCode;
using Application.Services.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Nam
    /// </summary>
    [ApiController]
    [Route("api/roles")]
    [Authorize(Policy = "AdminOnly")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _service;
        public RolesController(IRoleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Danh sách role (phân trang, lọc theo tên)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] RoleFilterRequest? filter)
        {
            var data = await _service.ListAsync(filter);
            return SuccessResp.Ok(data);
        }

        /// <summary>
        /// Chi tiết role theo Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id);
                return SuccessResp.Ok(data);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Tạo role
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
        {
            try
            {
                var data = await _service.CreateAsync(request);
                return SuccessResp.Created(data);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật role
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var data = await _service.UpdateAsync(id, request);
                return SuccessResp.Ok(data);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa role
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return SuccessResp.Ok(new { message = "Deleted" });
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }
    }
}


