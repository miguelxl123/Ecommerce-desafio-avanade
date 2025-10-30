using Ecommerce.Shared.Models;
using Ecommerce.Vendas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Vendas.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IVendasService _vendasService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IVendasService vendasService, ILogger<OrdersController> logger)
    {
        _vendasService = vendasService;
        _logger = logger;
    }
    /// Criar um novo pedido

    /// <param name="request">Dados do pedido</param>
    /// <returns>Pedido criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Order>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CriarPedido([FromBody] CreateOrderRequest request)
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

            var userId = GetUserIdFromClaims();
            var userEmail = GetUserEmailFromClaims();

            if (userId == Guid.Empty || string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(ApiResponse<object>.ErrorResult("Usuário não identificado"));
            }

            var result = await _vendasService.CriarPedidoAsync(request, userId, userEmail);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar pedido");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Listar pedidos do usuário autenticado
    /// </summary>
    /// <returns>Lista de pedidos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Order>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListarPedidos()
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var userRole = GetUserRoleFromClaims();

            // Administradores podem ver todos os pedidos, usuários comuns apenas os próprios
            var result = userRole == "Admin" 
                ? await _vendasService.ListarPedidosAsync()
                : await _vendasService.ListarPedidosAsync(userId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar pedidos");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Obter pedido por ID
    /// </summary>
    /// <param name="id">ID do pedido</param>
    /// <returns>Detalhes do pedido</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<Order>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ObterPedido(Guid id)
    {
        try
        {
            var result = await _vendasService.ObterPedidoPorIdAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            var userId = GetUserIdFromClaims();
            var userRole = GetUserRoleFromClaims();

            // Verificar se o usuário tem permissão para ver este pedido
            if (userRole != "Admin" && result.Data?.UserId != userId)
            {
                return Forbid();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pedido {OrderId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Confirmar um pedido
    /// </summary>
    /// <param name="id">ID do pedido</param>
    /// <returns>Pedido confirmado</returns>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<Order>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ConfirmarPedido(Guid id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var userRole = GetUserRoleFromClaims();

            // Verificar se o usuário tem permissão para confirmar o pedido
            var orderResult = await _vendasService.ObterPedidoPorIdAsync(id);
            if (!orderResult.Success)
            {
                return NotFound(orderResult);
            }

            if (userRole != "Admin" && orderResult.Data?.UserId != userId)
            {
                return Forbid();
            }

            var result = await _vendasService.ConfirmarPedidoAsync(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao confirmar pedido {OrderId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Cancelar um pedido
    /// </summary>
    /// <param name="id">ID do pedido</param>
    /// <returns>Pedido cancelado</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<Order>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CancelarPedido(Guid id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var userRole = GetUserRoleFromClaims();

            // Verificar se o usuário tem permissão para cancelar o pedido
            var orderResult = await _vendasService.ObterPedidoPorIdAsync(id);
            if (!orderResult.Success)
            {
                return NotFound(orderResult);
            }

            if (userRole != "Admin" && orderResult.Data?.UserId != userId)
            {
                return Forbid();
            }

            var result = await _vendasService.CancelarPedidoAsync(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar pedido {OrderId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Listar todos os pedidos (apenas para administradores)
    /// </summary>
    /// <returns>Lista de todos os pedidos</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Order>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> ListarTodosPedidos()
    {
        try
        {
            var result = await _vendasService.ListarPedidosAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar todos os pedidos");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         HttpContext.User.FindFirst("X-User-Id")?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private string GetUserEmailFromClaims()
    {
        return HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ??
               HttpContext.User.FindFirst("X-User-Email")?.Value ?? string.Empty;
    }

    private string GetUserRoleFromClaims()
    {
        return HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ??
               HttpContext.User.FindFirst("X-User-Role")?.Value ?? "User";
    }
}