using Application.DTOs.User;
using Application.ResponseCode;
using Application.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Quản lý người dùng (chỉ Admin) - Nam
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Danh sách user pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var data = await _userService.ListAsync(page, pageSize);
            return SuccessResp.Ok(data);
        }

        /// <summary>
        /// Xem chi tiết user theo Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            try
            {
                var data = await _userService.GetByIdAsync(id);
                return SuccessResp.Ok(data);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Tạo user
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            try
            {
                var data = await _userService.CreateAsync(request);
                return SuccessResp.Created(data);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update user (có thể cập nhật từng cái fullname, mssv, roles) , nào ko up thì khỏi điền
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var data = await _userService.UpdateAsync(id, request);
                return SuccessResp.Ok(data);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update status user (Active/Inactive/Locked)
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var data = await _userService.UpdateStatusAsync(id, request);
                return SuccessResp.Ok(data);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        // <summary>
        // Cập nhật roles của user (Admin only) , vd { "Roles": ["Admin", "Lecturer"] } , chưa có báo bug
        // </summary>
        //[HttpPatch("{id}/roles")]
        //public async Task<IActionResult> UpdateRoles([FromRoute] Guid id, [FromBody] UpdateUserRolesRequest request)
        //{
        //    try
        //    {
        //        var data = await _userService.UpdateRolesAsync(id, request);
        //        return SuccessResp.Ok(data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ErrorResp.BadRequest(ex.Message);
        //    }
        //}

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                return SuccessResp.NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
    }
}


