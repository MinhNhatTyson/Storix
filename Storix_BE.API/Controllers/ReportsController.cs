using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storix_BE.Service.Interfaces;
using System;
using System.Security.Claims;

namespace Storix_BE.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "1,2")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly IUserService _userService;

        public ReportsController(IReportingService reportingService, IUserService userService)
        {
            _reportingService = reportingService;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportApiRequest request)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var caller = await _userService.GetByEmailAsync(email);
            if (caller == null)
                return Unauthorized();

            if (roleId.Value == 2 && request.CompanyId.HasValue && request.CompanyId.Value > 0 && caller.CompanyId != request.CompanyId.Value)
                return StatusCode(403, new { message = "Cross-company access denied. Company Administrator can only access its own company." });

            var effectiveCompanyId = ResolveCompanyId(roleId.Value, caller.CompanyId, request.CompanyId);
            if (!effectiveCompanyId.HasValue)
            {
                if (roleId.Value == 1)
                    return BadRequest(new { message = "companyId is required for Super Admin." });
                return Unauthorized(new { message = "Missing companyId in token/user context." });
            }

            try
            {
                var payload = new CreateReportRequest(request.ReportType, request.WarehouseId, request.TimeFrom, request.TimeTo);
                var result = await _reportingService.CreateReportAsync(effectiveCompanyId.Value, caller.Id, payload);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReportById(int id, [FromQuery] int? companyId = null)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var caller = await _userService.GetByEmailAsync(email);
            if (caller == null)
                return Unauthorized();

            if (roleId.Value == 2 && companyId.HasValue && companyId.Value > 0 && caller.CompanyId != companyId.Value)
                return StatusCode(403, new { message = "Cross-company access denied. Company Administrator can only access its own company." });

            var effectiveCompanyId = ResolveCompanyId(roleId.Value, caller.CompanyId, companyId);
            if (!effectiveCompanyId.HasValue)
            {
                if (roleId.Value == 1)
                    return BadRequest(new { message = "companyId is required for Super Admin." });
                return Unauthorized(new { message = "Missing companyId in token/user context." });
            }

            try
            {
                var report = await _reportingService.GetReportAsync(effectiveCompanyId.Value, id);
                if (report == null) return NotFound(new { message = "Report not found." });
                return Ok(report);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListReports(
            [FromQuery] int? companyId = null,
            [FromQuery] string? type = null,
            [FromQuery] int? warehouseId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var caller = await _userService.GetByEmailAsync(email);
            if (caller == null)
                return Unauthorized();

            if (roleId.Value == 2 && companyId.HasValue && companyId.Value > 0 && caller.CompanyId != companyId.Value)
                return StatusCode(403, new { message = "Cross-company access denied. Company Administrator can only access its own company." });

            var effectiveCompanyId = ResolveCompanyId(roleId.Value, caller.CompanyId, companyId);
            if (!effectiveCompanyId.HasValue)
            {
                if (roleId.Value == 1)
                    return BadRequest(new { message = "companyId is required for Super Admin." });
                return Unauthorized(new { message = "Missing companyId in token/user context." });
            }

            try
            {
                var items = await _reportingService.ListReportsAsync(effectiveCompanyId.Value, type, warehouseId, from, to, skip, take);
                return Ok(items);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/export/pdf")]
        public async Task<IActionResult> ExportPdf(int id, [FromQuery] int? companyId = null)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            if (roleId == null || string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var caller = await _userService.GetByEmailAsync(email);
            if (caller == null)
                return Unauthorized();

            if (roleId.Value == 2 && companyId.HasValue && companyId.Value > 0 && caller.CompanyId != companyId.Value)
                return StatusCode(403, new { message = "Cross-company access denied. Company Administrator can only access its own company." });

            var effectiveCompanyId = ResolveCompanyId(roleId.Value, caller.CompanyId, companyId);
            if (!effectiveCompanyId.HasValue)
            {
                if (roleId.Value == 1)
                    return BadRequest(new { message = "companyId is required for Super Admin." });
                return Unauthorized(new { message = "Missing companyId in token/user context." });
            }

            try
            {
                var artifact = await _reportingService.ExportReportPdfAsync(effectiveCompanyId.Value, id);
                return Ok(artifact);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private int? GetRoleIdFromToken()
        {
            var roleIdStr = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrWhiteSpace(roleIdStr)) return null;
            return int.TryParse(roleIdStr, out var id) ? id : null;
        }

        private string? GetEmailFromToken()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value;
        }

        private static int? ResolveCompanyId(int roleId, int? callerCompanyId, int? requestedCompanyId)
        {
            // Super Admin can query any company but must specify it explicitly.
            if (roleId == 1)
            {
                return requestedCompanyId.HasValue && requestedCompanyId.Value > 0
                    ? requestedCompanyId.Value
                    : null;
            }

            // Company Admin: always scoped to its own company; allow requestedCompanyId only if it matches.
            if (!callerCompanyId.HasValue || callerCompanyId.Value <= 0) return null;

            if (requestedCompanyId.HasValue && requestedCompanyId.Value > 0 && requestedCompanyId.Value != callerCompanyId.Value)
                return null;

            return callerCompanyId.Value;
        }
    }

    public sealed record CreateReportApiRequest(
        string ReportType,
        int? WarehouseId,
        DateTime TimeFrom,
        DateTime TimeTo,
        int? CompanyId);
}

