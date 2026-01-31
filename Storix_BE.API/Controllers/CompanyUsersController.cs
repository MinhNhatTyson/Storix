using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storix_BE.Repository.DTO;
using Storix_BE.Service.Interfaces;
using CreateUserRequest = Storix_BE.Service.Interfaces.CreateUserRequest;
using UpdateUserRequest = Storix_BE.Service.Interfaces.UpdateUserRequest;

namespace Storix_BE.API.Controllers
{
    /// <summary>
    /// CRUD accounts within the company. Only Company Administrator can use these endpoints.
    /// Manager and Staff cannot register; they are created by Company Administrator.
    /// </summary>
    [ApiController]
    [Route("api/company-users/{companyId:int}")]
    [Authorize]
    [Tags("Company Users")]
    public class CompanyUsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public CompanyUsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPut("update-profile/{userId}")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> UpdateProfile(int companyId, int userId, [FromBody] UpdateProfileDto dto)
        {
            try
            {
                var tokenCompanyId = GetCompanyIdFromToken();
                if (tokenCompanyId == null || tokenCompanyId.Value != companyId)
                    return Unauthorized();
                if (dto.CompanyId.HasValue && dto.CompanyId.Value != tokenCompanyId.Value)
                    return BadRequest(new { message = "CompanyId does not match the authenticated company." });

                var updatedUser = await _userService.UpdateProfileAsync(userId, dto);
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        private int? GetCompanyIdFromToken()
        {
            var companyIdStr = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(companyIdStr)) return null;
            return int.TryParse(companyIdStr, out var id) ? id : null;
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
        /// List all users in the current company. Company Administrator only.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(int companyId)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            var tokenCompanyId = GetCompanyIdFromToken();
            if (roleId == null || string.IsNullOrEmpty(email) || tokenCompanyId == null || tokenCompanyId.Value != companyId)
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null || caller.CompanyId.Value != companyId)
                    return Unauthorized();
                var users = await _userService.GetUsersByCompanyAsync(companyId, roleId.Value);
                return Ok(users);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Get a user by id (must belong to your company). Company Administrator only.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int companyId, int id)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            var tokenCompanyId = GetCompanyIdFromToken();
            if (roleId == null || string.IsNullOrEmpty(email) || tokenCompanyId == null || tokenCompanyId.Value != companyId)
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null || caller.CompanyId.Value != companyId)
                    return Unauthorized();
                var users = await _userService.GetUsersByCompanyAsync(companyId, roleId.Value);
                var user = users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    return NotFound();
                return Ok(user);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Create a new user (Manager or Staff only). Company Administrator only.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser(int companyId, [FromBody] CreateUserRequest request)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            var tokenCompanyId = GetCompanyIdFromToken();
            if (roleId == null || string.IsNullOrEmpty(email) || tokenCompanyId == null || tokenCompanyId.Value != companyId)
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null || caller.CompanyId.Value != companyId)
                    return Unauthorized();
                var user = await _userService.CreateUserAsync(companyId, roleId.Value, request);
                return Ok(user);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update a user (Manager or Staff). Company Administrator only.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int companyId, int id, [FromBody] UpdateUserRequest request)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            var tokenCompanyId = GetCompanyIdFromToken();
            if (roleId == null || string.IsNullOrEmpty(email) || tokenCompanyId == null || tokenCompanyId.Value != companyId)
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null || caller.CompanyId.Value != companyId)
                    return Unauthorized();
                var user = await _userService.UpdateUserAsync(id, companyId, roleId.Value, caller.Id, request);
                if (user == null)
                    return NotFound();
                return Ok(user);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a user (Manager or Staff only; cannot delete Company Administrator). Company Administrator only.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int companyId, int id)
        {
            var roleId = GetRoleIdFromToken();
            var email = GetEmailFromToken();
            var tokenCompanyId = GetCompanyIdFromToken();
            if (roleId == null || string.IsNullOrEmpty(email) || tokenCompanyId == null || tokenCompanyId.Value != companyId)
                return Unauthorized();

            try
            {
                var caller = await _userService.GetByEmailAsync(email);
                if (caller?.CompanyId == null || caller.CompanyId.Value != companyId)
                    return Unauthorized();

                var deleted = await _userService.DeleteUserAsync(id, companyId, roleId.Value, caller.Id);
                if (!deleted)
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
        }
    }
}
