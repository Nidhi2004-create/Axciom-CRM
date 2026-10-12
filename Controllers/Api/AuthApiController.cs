using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _auditService = auditService;
        }

        /// <summary>
        /// Authenticates user and returns JWT Bearer token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] ApiLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                await _auditService.LogAsync(null, request.Email, "Failed Login", "API", result: "Failure", details: "API authentication failed: Invalid account or deactivated.");
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API", recordId: user.Id, result: "LockedOut", details: "API account lockout triggered.");
                return Unauthorized(new { message = "Account is locked due to multiple failed login attempts." });
            }

            if (!result.Succeeded)
            {
                await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API", recordId: user.Id, result: "Failure", details: "API authentication failed: Invalid password.");
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // Generate JWT Token
            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.FirstOrDefault() ?? "SalesExecutive";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email ?? user.UserName!),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, primaryRole),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "AcxiomCRM_SuperSecretKey_For_Jwt_Authentication_Secure2026!#"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpiryInHours"] ?? "12"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "AcxiomCRM",
                audience: _configuration["Jwt:Audience"] ?? "AcxiomCRMClient",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            await _auditService.LogAsync(user.Id, user.Email, "Login", "API", recordId: user.Id, result: "Success", details: "JWT token issued via REST API.");

            return Ok(new ApiLoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expires,
                User = new ApiUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = primaryRole
                }
            });
        }

        /// <summary>
        /// Terminates authenticated API session
        /// </summary>
        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            await _auditService.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.Identity?.Name,
                "Logout",
                "API",
                result: "Success",
                details: "API session logout completed.");

            return Ok(new { message = "Successfully logged out." });
        }
    }
}
