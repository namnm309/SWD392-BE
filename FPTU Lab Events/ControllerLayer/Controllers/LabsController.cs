using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Lab;
using Application.ResponseCode;
using Application.Services.Lab;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Lab Management Controller
    /// </summary>
    [ApiController]
    [Route("api/labs")]
    [Authorize]
    public class LabsController : ControllerBase
    {
        private readonly ILabService _labService;

        public LabsController(ILabService labService)
        {
            _labService = labService;
        }

        /// <summary>
        /// Lấy tất cả lab (View Lab API)
        /// AC-01: System must display list of all labs with details
        /// AC-02: Each lab must show: Lab ID, Name, Location, Capacity, Available Equipment, Status (Active/Inactive)
        /// AC-03: User can filter labs by location or capacity
        /// AC-04: User can view detailed information of each lab
        /// AC-05: Display message "No labs available" if list is empty
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllLabs([FromQuery] LabFilterRequest? filter)
        {
            try
            {
                var labs = await _labService.GetAllLabsAsync(filter);
                
                if (labs.Count == 0)
                {
                    return SuccessResp.Ok(new { Message = "No labs available", Labs = labs });
                }
                
                return SuccessResp.Ok(labs);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy lab theo ID (View Lab Detail)
        /// AC-04: User can view detailed information of each lab
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLabById(Guid id)
        {
            try
            {
                var lab = await _labService.GetLabByIdAsync(id);
                return SuccessResp.Ok(lab);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Lấy lab có sẵn (Available Labs)
        /// </summary>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableLabs()
        {
            try
            {
                var labs = await _labService.GetAvailableLabsAsync();
                
                if (labs.Count == 0)
                {
                    return SuccessResp.Ok(new { Message = "No labs available", Labs = labs });
                }
                
                return SuccessResp.Ok(labs);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Kiểm tra lab có sẵn không
        /// </summary>
        [HttpGet("{id}/available")]
        public async Task<IActionResult> IsLabAvailable(Guid id)
        {
            try
            {
                var isAvailable = await _labService.IsLabAvailableAsync(id);
                return SuccessResp.Ok(new { IsAvailable = isAvailable });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng lab
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetLabCount()
        {
            try
            {
                var count = await _labService.GetLabCountAsync();
                return SuccessResp.Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng lab đang hoạt động
        /// </summary>
        [HttpGet("active-count")]
        public async Task<IActionResult> GetActiveLabCount()
        {
            try
            {
                var count = await _labService.GetActiveLabCountAsync();
                return SuccessResp.Ok(new { ActiveCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Tạo lab mới (Admin only)
        /// AC-01: System must display fields: Lab Name, Description, Location, Capacity, Room, Status
        /// AC-02: Required fields (Lab Name, Capacity, Room) cannot be null
        /// AC-03: Upon successful submission, save lab to database
        /// AC-04: Display message: "Create lab successfully"
        /// AC-05: Log creation event with admin ID, timestamp, and lab ID
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateLab([FromBody] CreateLabRequest request)
        {
            try
            {
                // Get admin ID from JWT token
                var adminIdClaim = User.FindFirst("sub") ?? User.FindFirst("id");
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                    return ErrorResp.Unauthorized("Invalid admin ID");

                var lab = await _labService.CreateLabAsync(request, adminId);
                return SuccessResp.Created(new { Message = "Create lab successfully", Lab = lab });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật lab (Admin only)
        /// AC-01: System must display editable fields: Lab Name, Description, Location, Capacity, Status
        /// AC-02: Required fields cannot be null
        /// AC-03: Upon valid submission, update lab information
        /// AC-04: Display message: "Update lab successfully"
        /// AC-05: Log edit event with admin ID, lab ID, timestamp, and changes
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLab(Guid id, [FromBody] UpdateLabRequest request)
        {
            try
            {
                // Get admin ID from JWT token
                var adminIdClaim = User.FindFirst("sub") ?? User.FindFirst("id");
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                    return ErrorResp.Unauthorized("Invalid admin ID");

                var lab = await _labService.UpdateLabAsync(id, request, adminId);
                return SuccessResp.Ok(new { Message = "Update lab successfully", Lab = lab });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái lab (Admin only)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLabStatus(Guid id, [FromBody] UpdateLabStatusRequest request)
        {
            try
            {
                var lab = await _labService.UpdateLabStatusAsync(id, request);
                return SuccessResp.Ok(lab);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa lab (Admin only)
        /// AC-01: System must check if lab has pending or approved bookings
        /// AC-02: If bookings exist, display warning: "Cannot delete lab with active bookings"
        /// AC-03: Admin must confirm deletion before removing
        /// AC-04: Upon confirmation, delete lab from system
        /// AC-05: Display message: "Delete lab successfully"
        /// AC-06: Log deletion event with admin ID, lab ID, and timestamp
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLab(Guid id, [FromBody] DeleteLabRequest request)
        {
            try
            {
                // Get admin ID from JWT token
                var adminIdClaim = User.FindFirst("sub") ?? User.FindFirst("id");
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                    return ErrorResp.Unauthorized("Invalid admin ID");

                await _labService.DeleteLabAsync(id, request, adminId);
                return SuccessResp.Ok(new { Message = "Delete lab successfully" });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
    }
}
