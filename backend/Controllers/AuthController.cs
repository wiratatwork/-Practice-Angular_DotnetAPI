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
        private readonly RefreshTokenService _refreshTokenService;
        private readonly RefreshTokenCookieService _cookieService;
        private readonly AuthAuditService _auditService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            JwtTokenService jwtTokenService,
            RefreshTokenService refreshTokenService,
            RefreshTokenCookieService cookieService,
            AuthAuditService auditService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
            _cookieService = cookieService;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var submittedUsername = request?.Username?.Trim();

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

                var normalizedUsername = submittedUsername!.ToLower();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    await _auditService.LogAsync(
                        AuthAuditEventTypes.LoginFailure,
                        succeeded: false,
                        username: submittedUsername,
                        failureReason: "Invalid username or password");

                    return Unauthorized(new { message = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง" });
                }

                var (accessToken, expiresIn) = _jwtTokenService.GenerateAccessToken(user);
                var refreshResult = await _refreshTokenService.CreateAsync(
                    user,
                    GetClientIp(),
                    GetUserAgent());

                var refreshToken = await _context.RefreshTokens
                    .SingleAsync(rt => rt.Id == refreshResult.RefreshTokenId);

                _cookieService.SetRefreshTokenCookie(
                    Response,
                    refreshResult.RawToken!,
                    refreshToken.ExpiresAtUtc);

                await _auditService.LogAsync(
                    AuthAuditEventTypes.LoginSuccess,
                    succeeded: true,
                    username: user.Username,
                    userId: user.Id,
                    refreshTokenId: refreshResult.RefreshTokenId);

                return Ok(BuildTokenResponse<LoginResponse>(accessToken, expiresIn, user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                await _auditService.LogAsync(
                    AuthAuditEventTypes.LoginFailure,
                    succeeded: false,
                    username: submittedUsername,
                    failureReason: "Internal server error");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<RefreshResponse>> Refresh()
        {
            var rawToken = _cookieService.GetRefreshTokenFromRequest(Request);

            try
            {
                if (string.IsNullOrWhiteSpace(rawToken))
                {
                    await _auditService.LogAsync(
                        AuthAuditEventTypes.RefreshFailure,
                        succeeded: false,
                        failureReason: "Refresh token cookie missing");

                    return Unauthorized(new { message = "Refresh token is missing" });
                }

                var rotateResult = await _refreshTokenService.RotateAsync(
                    rawToken,
                    GetClientIp(),
                    GetUserAgent());

                if (!rotateResult.Success || rotateResult.User == null)
                {
                    await _auditService.LogAsync(
                        AuthAuditEventTypes.RefreshFailure,
                        succeeded: false,
                        username: rotateResult.User?.Username,
                        userId: rotateResult.User?.Id,
                        failureReason: rotateResult.FailureReason,
                        refreshTokenId: rotateResult.RefreshTokenId,
                        metadata: rotateResult.ReuseDetected
                            ? new { rotateResult.ReuseDetected }
                            : null);

                    return Unauthorized(new { message = rotateResult.FailureReason ?? "Invalid refresh token" });
                }

                var refreshToken = await _context.RefreshTokens
                    .SingleAsync(rt => rt.Id == rotateResult.RefreshTokenId);

                _cookieService.SetRefreshTokenCookie(
                    Response,
                    rotateResult.RawToken!,
                    refreshToken.ExpiresAtUtc);

                var (accessToken, expiresIn) = _jwtTokenService.GenerateAccessToken(rotateResult.User);

                await _auditService.LogAsync(
                    AuthAuditEventTypes.RefreshSuccess,
                    succeeded: true,
                    username: rotateResult.User.Username,
                    userId: rotateResult.User.Id,
                    refreshTokenId: rotateResult.RefreshTokenId,
                    metadata: new { slidingExpiresAtUtc = refreshToken.ExpiresAtUtc });

                return Ok(BuildTokenResponse<RefreshResponse>(accessToken, expiresIn, rotateResult.User));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refresh");
                await _auditService.LogAsync(
                    AuthAuditEventTypes.RefreshFailure,
                    succeeded: false,
                    failureReason: "Internal server error");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            var rawToken = _cookieService.GetRefreshTokenFromRequest(Request);

            try
            {
                if (!string.IsNullOrWhiteSpace(rawToken))
                {
                    var revokeResult = await _refreshTokenService.RevokeAsync(rawToken, "User logout");

                    await _auditService.LogAsync(
                        revokeResult.Success ? AuthAuditEventTypes.LogoutSuccess : AuthAuditEventTypes.LogoutFailure,
                        succeeded: revokeResult.Success,
                        username: revokeResult.User?.Username,
                        userId: revokeResult.User?.Id,
                        failureReason: revokeResult.FailureReason,
                        refreshTokenId: revokeResult.RefreshTokenId);
                }
                else
                {
                    await _auditService.LogAsync(
                        AuthAuditEventTypes.LogoutSuccess,
                        succeeded: true,
                        failureReason: "No refresh token cookie present");
                }

                _cookieService.ClearRefreshTokenCookie(Response);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                await _auditService.LogAsync(
                    AuthAuditEventTypes.LogoutFailure,
                    succeeded: false,
                    failureReason: "Internal server error");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private static TResponse BuildTokenResponse<TResponse>(string accessToken, int expiresIn, User user)
            where TResponse : TokenResponse, new()
        {
            return new TResponse
            {
                AccessToken = accessToken,
                ExpiresIn = expiresIn,
                User = new UserDto
                {
                    Username = user.Username,
                    Role = user.Role,
                },
            };
        }

        private string? GetClientIp()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return Request.Headers.UserAgent.ToString();
        }
    }
}
