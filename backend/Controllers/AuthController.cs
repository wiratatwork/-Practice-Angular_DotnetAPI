using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            JwtTokenService jwtTokenService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Login with username and password
        /// POST: api/auth/login
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Login credentials are required" });
                }

                var validationContext = new ValidationContext(request);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
                {
                    var errors = validationResults.Select(r => r.ErrorMessage).ToList();
                    return BadRequest(new { message = "Validation failed", errors });
                }

                var normalizedUsername = request.Username.Trim().ToLower();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง" });
                }

                var token = _jwtTokenService.GenerateToken(user);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
