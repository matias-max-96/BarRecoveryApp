using BarRecoveryApp.Api.Dtos;
using BarRecoveryApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarRecoveryApp.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;

        public AuthController(
            IJwtTokenService jwtTokenService,
            IConfiguration configuration)
        {
            _jwtTokenService = jwtTokenService
                ?? throw new ArgumentNullException(nameof(jwtTokenService));

            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            var expectedUsername = _configuration["PilotAuth:Username"];
            var expectedPassword = _configuration["PilotAuth:Password"];

            if (string.IsNullOrWhiteSpace(expectedUsername) ||
                string.IsNullOrWhiteSpace(expectedPassword))
            {
                return StatusCode(500,
                    "PilotAuth:Username / PilotAuth:Password no están configurados en el servidor.");
            }

            if (request.Username != expectedUsername || request.Password != expectedPassword)
            {
                return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
            }

            var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(
                userId: "pilot-user",
                username: request.Username);

            return Ok(new LoginResponseDto
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc
            });
        }
    }
}
