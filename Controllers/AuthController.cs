using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            var user = _authService.Register(
                request.Username,
                request.Email,
                request.Password
            );

            if (user == null)
                return BadRequest("الإيميل مسجل مسبقاً");

            return Ok(new { message = "تم التسجيل بنجاح" });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var token = _authService.Login(request.Email, request.Password);

            if (token == null)
                return Unauthorized("إيميل أو باسورد غلط");

            return Ok(new { token });
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}