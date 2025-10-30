using ApiGateway.Models;
using ApiGateway.Services;
using Ecommerce.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// Realizar login no sistema
    /// <param name="request">Dados de login</param>
    /// <returns>Token JWT e informações do usuário</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResult("Dados inválidos", errors));
            }

            var result = await _authService.LoginAsync(request);

            if (result == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResult("Email ou senha inválidos"));
            }

            return Ok(ApiResponse<LoginResponse>.SuccessResult(result, "Login realizado com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Registrar novo usuário
    /// </summary>
    /// <param name="request">Dados de registro</param>
    /// <returns>Token JWT e informações do usuário</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResult("Dados inválidos", errors));
            }

            var result = await _authService.RegisterAsync(request);

            if (result == null)
            {
                return Conflict(ApiResponse<object>.ErrorResult("Email já está em uso"));
            }

            return Ok(ApiResponse<LoginResponse>.SuccessResult(result, "Usuário registrado com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Validar token JWT
    /// </summary>
    /// <returns>Informações do usuário autenticado</returns>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Validate()
    {
        try
        {
            var userId = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var userInfo = new
            {
                Id = userId,
                Email = email,
                Name = name,
                Role = role
            };

            return Ok(ApiResponse<object>.SuccessResult(userInfo, "Token válido"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Realizar logout (invalidar token)
    /// </summary>
    /// <returns>Confirmação de logout</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Em uma implementação real, você adicionaria o token a uma blacklist
            // ou implementaria um sistema de revogação de tokens

            var email = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            _logger.LogInformation("User {Email} logged out", email);

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Logout realizado com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }
}