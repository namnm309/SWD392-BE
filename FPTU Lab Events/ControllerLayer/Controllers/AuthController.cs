using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Auth;
using Application.ResponseCode;
using Application.Services.Auth;
using InfrastructureLayer.Core.JWT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Nam
    /// </summary>
    [ApiController]
    [Route("api/auth")]    
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthService authService, IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Đăng ký tài khoản mới (kiểu tk bth ko mail FPT) (mặc định gán role Student).
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var resp = await _authService.RegisterAsync(request);
                return SuccessResp.Created(resp);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Đăng nhập bằng username hoặc email và mật khẩu.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var resp = await _authService.LoginAsync(request);
                return SuccessResp.Ok(resp);
            }
            catch (Exception ex)
            {
                return ErrorResp.Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Đăng nhập Google bằng id_token (simple).
        /// </summary>
        [HttpPost("google/token")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleToken([FromBody] GoogleTokenRequest request)
        {
            try
            {
                var allowed = new[] { "fpt.edu.vn", "fe.edu.vn" };
                var tokens = await _authService.LoginWithGoogleIdTokenAsync(request.IdToken, allowed);
                return SuccessResp.Ok(tokens);
            }
            catch (Exception ex)
            {
                return ErrorResp.Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Lấy access token 
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            try
            {
                var device = Request.Headers["User-Agent"].FirstOrDefault();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var resp = await _authService.RefreshAsync(request.RefreshToken, device, ip);
                return SuccessResp.Ok(resp);
            }
            catch (Exception ex)
            {
                return ErrorResp.Unauthorized(ex.Message);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var sessionIdStr = User.Claims.FirstOrDefault(c => c.Type == "sessionId")?.Value;
                if (string.IsNullOrEmpty(sessionIdStr)) return ErrorResp.BadRequest("Session not found");
                await _authService.LogoutAsync(Guid.Parse(sessionIdStr));
                return SuccessResp.NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Thông tin hiện tại qua token.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdStr)) return ErrorResp.BadRequest("User not found");
                var me = await _authService.GetMeAsync(Guid.Parse(userIdStr));
                return SuccessResp.Ok(me);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
                
        /// <summary>
        /// C1(step1):Fe call -> nhận URL -> redirect user qua cái URL này.
        /// </summary>
        [HttpGet("google/start")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleStart([FromQuery] string redirectUri, [FromQuery] string state)
        {
            var url = await _authService.GetGoogleAuthorizationUrlAsync(redirectUri, state);
            return SuccessResp.Ok(new GoogleAuthStartResponse { AuthorizationUrl = url });
        }

        /// <summary>
        /// C1(step2):convert từ cái google trả -> id_token -> verify -> trả jwt.
        /// </summary>
        [HttpPost("google/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback([FromBody] GoogleCallbackRequest request)
        {
            try
            {
                // chỉ cho domain FPT
                var allowed = new[] { "fpt.edu.vn", "fe.edu.vn" };
                var tokens = await _authService.HandleGoogleCallbackAsync(request.Code, request.RedirectUri, allowed);
                return SuccessResp.Ok(tokens);
            }
            catch (Exception ex)
            {
                return ErrorResp.Unauthorized(ex.Message);
            }
        }
        
       
    }
}


