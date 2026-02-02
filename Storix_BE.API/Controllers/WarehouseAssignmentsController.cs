using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storix_BE.API.DTO;
using Storix_BE.Domain.Models;
using Storix_BE.Service.Interfaces;
using System.Linq;
using AssignWarehouseRequest = Storix_BE.Service.Interfaces.AssignWarehouseRequest;

namespace Storix_BE.API.Controllers
{
    [ApiController]
    [Route("api/company-warehouses/{companyId:int}/assignments")]
    [Authorize(Roles = "2")]
    public class WarehouseAssignmentsController : ControllerBase
    {
        private readonly IWarehouseAssignmentService _assignmentService;
        private readonly IUserService _userService;

        public WarehouseAssignmentsController(IWarehouseAssignmentService assignmentService, IUserService userService)
        {
            _assignmentService = assignmentService;
            _userService = userService;
        }

        private int? GetRoleIdFromToken()
        {
            var roleIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleIdStr)) return null;
            return int.TryParse(roleIdStr, out var id) ? id : null;
        }

        private string? GetEmailFromToken()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// List all warehouse assignments within the current company. Company Administrator only.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAssignments(int companyId)
        {
            if (companyId <= 0)
                return BadRequest(new { message = "CompanyId is required." });
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrEmpty(email))
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null)
                    return Unauthorized();
                if (caller.CompanyId.Value != companyId)
                    return Forbid();
                var assignments = await _assignmentService.GetAssignmentsByCompanyAsync(companyId, roleId.Value);
                return Ok(assignments.Select(MapAssignment));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// List Manager/Staff assigned to a specific warehouse (within your company).
        /// </summary>
        [HttpGet("warehouse/{warehouseId:int}")]
        public async Task<IActionResult> GetAssignmentsByWarehouse(int companyId, int warehouseId)
        {
            if (companyId <= 0)
                return BadRequest(new { message = "CompanyId is required." });
            if (warehouseId <= 0)
                return BadRequest(new { message = "WarehouseId is required." });
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrEmpty(email))
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null)
                    return Unauthorized();
                if (caller.CompanyId.Value != companyId)
                    return Forbid();
                var assignments = await _assignmentService.GetAssignmentsByWarehouseAsync(
                    companyId,
                    roleId.Value,
                    warehouseId);
                return Ok(assignments.Select(MapAssignment));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Assign a warehouse to a Manager/Staff. Company Administrator only.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssignWarehouse(int companyId, [FromBody] AssignWarehouseRequest request)
        {
            if (companyId <= 0)
                return BadRequest(new { message = "CompanyId is required." });
            if (request == null || request.UserId <= 0 || request.WarehouseId <= 0)
                return BadRequest(new { message = "UserId and WarehouseId are required." });
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrEmpty(email))
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null)
                    return Unauthorized();
                if (caller.CompanyId.Value != companyId)
                    return Forbid();
                var assignment = await _assignmentService.AssignWarehouseAsync(companyId, roleId.Value, request);
                return Ok(MapAssignment(assignment));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Unassign a user from a warehouse. Company Administrator only.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> UnassignWarehouse(int companyId, [FromQuery] int userId, [FromQuery] int warehouseId)
        {
            if (companyId <= 0)
                return BadRequest(new { message = "CompanyId is required." });
            if (userId <= 0 || warehouseId <= 0)
                return BadRequest(new { message = "UserId and WarehouseId are required." });
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrEmpty(email))
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null)
                    return Unauthorized();
                if (caller.CompanyId.Value != companyId)
                    return Forbid();
                var removed = await _assignmentService.UnassignWarehouseAsync(companyId, roleId.Value, userId, warehouseId);
                if (!removed)
                    return NotFound();
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static WarehouseAssignmentResponseDto MapAssignment(WarehouseAssignment assignment)
        {
            return new WarehouseAssignmentResponseDto(
                assignment.Id,
                assignment.UserId,
                assignment.WarehouseId,
                assignment.RoleInWarehouse,
                assignment.AssignedAt,
                assignment.User == null
                    ? null
                    : new UserSummaryDto(
                        assignment.User.Id,
                        assignment.User.CompanyId,
                        assignment.User.FullName,
                        assignment.User.Email,
                        assignment.User.Phone,
                        assignment.User.RoleId,
                        assignment.User.Role?.Name,
                        assignment.User.Status,
                        assignment.User.CreatedAt,
                        assignment.User.UpdatedAt),
                assignment.Warehouse == null
                    ? null
                    : new WarehouseSummaryDto(
                        assignment.Warehouse.Id,
                        assignment.Warehouse.CompanyId,
                        assignment.Warehouse.Name,
                        assignment.Warehouse.Status)
            );
        }
    }
}
