using AuthApi.Models;
using AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(ILogger<AuthController> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(UserDto dto)
        {
            await _authService.RegisterAsync(dto);
            return Created();
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            TokenDto token = await _authService.AuthenticateAsync(dto, ip);
            return Ok(token);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh(RefreshRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            TokenDto token = await _authService.RefreshTokenAsync(request.RefreshToken, ip);
            return Ok(token);
        }

        [Authorize]
        [HttpGet("secret")]
        public ActionResult<string> GetSecret() => Ok("You are authorized!");

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-data")]
        public IActionResult GetAdminData() => Ok("Only admins see this");
    }
}
