using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Equipment;
using Application.ResponseCode;
using Application.Services.Equipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    [ApiController]
    [Route("api/equipments")]
    [Authorize]
    public class EquipmentsController : ControllerBase
    {
        private readonly IEquipmentService _equipmentService;

        public EquipmentsController(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        /// <summary>
        /// Lấy tất cả thiết bị
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEquipments([FromQuery] EquipmentFilterRequest? filter)
        {
            try
            {
                var equipments = await _equipmentService.GetAllEquipmentsAsync(filter);
                return SuccessResp.Ok(equipments);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy thiết bị theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEquipmentById(Guid id)
        {
            try
            {
                var equipment = await _equipmentService.GetEquipmentByIdAsync(id);
                return SuccessResp.Ok(equipment);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Tạo thiết bị mới (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEquipment([FromBody] CreateEquipmentRequest request)
        {
            try
            {
                var equipment = await _equipmentService.CreateEquipmentAsync(request);
                return SuccessResp.Created(equipment);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật thiết bị (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEquipment(Guid id, [FromBody] UpdateEquipmentRequest request)
        {
            try
            {
                var equipment = await _equipmentService.UpdateEquipmentAsync(id, request);
                return SuccessResp.Ok(equipment);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái thiết bị (Admin only)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEquipmentStatus(Guid id, [FromBody] UpdateEquipmentStatusRequest request)
        {
            try
            {
                var equipment = await _equipmentService.UpdateEquipmentStatusAsync(id, request);
                return SuccessResp.Ok(equipment);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa thiết bị (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEquipment(Guid id)
        {
            try
            {
                await _equipmentService.DeleteEquipmentAsync(id);
                return SuccessResp.NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy thiết bị theo phòng
        /// </summary>
        [HttpGet("by-room/{roomId}")]
        public async Task<IActionResult> GetEquipmentsByRoom(Guid roomId)
        {
            try
            {
                var equipments = await _equipmentService.GetEquipmentsByRoomAsync(roomId);
                return SuccessResp.Ok(equipments);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy thiết bị có sẵn
        /// </summary>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableEquipments()
        {
            try
            {
                var equipments = await _equipmentService.GetAvailableEquipmentsAsync();
                return SuccessResp.Ok(equipments);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy thiết bị cần bảo trì
        /// </summary>
        [HttpGet("maintenance-needed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEquipmentsNeedingMaintenance()
        {
            try
            {
                var equipments = await _equipmentService.GetEquipmentsNeedingMaintenanceAsync();
                return SuccessResp.Ok(equipments);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng thiết bị
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetEquipmentCount()
        {
            try
            {
                var count = await _equipmentService.GetEquipmentCountAsync();
                return SuccessResp.Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng thiết bị có sẵn
        /// </summary>
        [HttpGet("available-count")]
        public async Task<IActionResult> GetAvailableEquipmentCount()
        {
            try
            {
                var count = await _equipmentService.GetAvailableEquipmentCountAsync();
                return SuccessResp.Ok(new { AvailableCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
    }
}
