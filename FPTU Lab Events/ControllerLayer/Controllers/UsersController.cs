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
        /// Lấy danh sách người dùng với phân trang (chỉ dành cho Admin).
        /// </summary>
        /// <param name="page">Số trang (bắt đầu từ 0).</param>
        /// <param name="pageSize">Số lượng bản ghi mỗi trang.</param>
        /// <returns>Danh sách người dùng với thông tin cơ bản.</returns>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var data = await _userService.ListAsync(page, pageSize);
            return SuccessResp.Ok(data);
        }

        /// <summary>
        /// Lấy thông tin chi tiết người dùng theo ID (chỉ dành cho Admin).
        /// </summary>
        /// <param name="id">ID của người dùng cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết người dùng bao gồm roles và trạng thái.</returns>
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
        /// Tạo người dùng mới (chỉ dành cho Admin).
        /// </summary>
        /// <param name="request">Thông tin tạo người dùng bao gồm username, email, fullname, mssv và roles.</param>
        /// <returns>Thông tin người dùng vừa tạo.</returns>
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
        /// Cập nhật thông tin người dùng (chỉ dành cho Admin).
        /// </summary>
        /// <param name="id">ID của người dùng cần cập nhật.</param>
        /// <param name="request">Thông tin cập nhật (chỉ điền các trường cần thay đổi).</param>
        /// <returns>Thông tin người dùng sau khi cập nhật.</returns>
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
        /// Cập nhật trạng thái người dùng (chỉ dành cho Admin).
        /// </summary>
        /// <param name="id">ID của người dùng cần cập nhật trạng thái.</param>
        /// <param name="request">Trạng thái mới (Active=0, Inactive=1, Locked=2).</param>
        /// <returns>Thông tin người dùng sau khi cập nhật trạng thái.</returns>
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

        /// <summary>
        /// Xóa người dùng (chỉ dành cho Admin).
        /// </summary>
        /// <param name="id">ID của người dùng cần xóa.</param>
        /// <returns>Không có nội dung trả về (204 No Content).</returns>
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


