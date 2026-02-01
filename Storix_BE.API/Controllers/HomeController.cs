using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Storix_BE.Domain.Models;
using Storix_BE.Service.Interfaces;
using System.ComponentModel.Design;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Storix_BE.API.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserService _accService;
        private readonly IConfiguration _config;
        public HomeController(IConfiguration config, IUserService accService)
        {
            _config = config;
            _accService = accService;
        }
        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _accService.Login(request.Email, request.Password);
            
            if (user == null || user.Result == null)
                return Unauthorized();

            var token = GenerateJSONWebToken(user.Result);

            return Ok(new
            {
                Token = token,
                RoleId = user.Result.RoleId,
                CompanyId = user.Result.CompanyId
            });
        }
        [HttpPost("Signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            try
            {
                var user = await _accService.SignupNewAccount(request.FullName, request.Email, request.PhoneNumber, request.Password, request.Address, request.CompanyCode);
                return CreatedAtAction(nameof(Login), new { email = user.Email }, user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpGet("login-google")]
        public IResult LoginWithGoogle([FromQuery] string returnURL, LinkGenerator linkGenerator, SignInManager<User> signManager, HttpContext context)
        {
            //tà đạo
            if (returnURL == null) returnURL = "https://localhost:5173";
            //
            var properties = signManager.ConfigureExternalAuthenticationProperties("Google", linkGenerator.GetPathByName(context, "GoogleLoginCallback" + $"?returnURL={returnURL}"));

            return Results.Challenge(properties, ["Google"]);
        }
        [HttpGet("login-google-callback", Name = "GoogleLoginCallback")]
        public async Task<IActionResult> LoginWithGoogleCallback([FromQuery] string returnURL, HttpContext context, IUserService service)
        {
            var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            var user = service.LoginWithGoogleAsync(result.Principal);
            var token = GenerateJSONWebToken(user.Result);
            /*return Results.Redirect(returnURL);*/
            return Ok(new
            {
                Token = token,
                RoleId = user.Result.RoleId
            });
        }
        private string GenerateJSONWebToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_config["Jwt:Issuer"]
                    , _config["Jwt:Audience"]
                    , new Claim[]
                    {
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.RoleId.ToString()),
                    },
                    expires: DateTime.Now.AddMinutes(120),
                    signingCredentials: credentials
                );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenString;
        }

        public sealed record LoginRequest(string Email, string Password);
        public sealed record SignupRequest(string FullName, string Email, string PhoneNumber, string Password, string Address, string CompanyCode);
    }
}
