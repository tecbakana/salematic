using Microsoft.AspNetCore.Mvc;
using Salematic.Application.DTOs;
using Salematic.Application.Services;

namespace Salematic.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ClienteService _clienteService;

        public AuthController(ClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _clienteService.RegisterAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _clienteService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EsqueceuSenhaRequest request)
        {
            await _clienteService.EsqueceuSenhaAsync(request);
            return Ok(new { message = "Se o e-mail estiver cadastrado, você receberá as instruções em breve." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] RedefinirSenhaRequest request)
        {
            try
            {
                await _clienteService.RedefinirSenhaAsync(request);
                return Ok(new { message = "Senha redefinida com sucesso." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
