using Ecommerce.Shared.Models;
using Ecommerce.Estoque.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Estoque.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IEstoqueService _estoqueService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IEstoqueService estoqueService, ILogger<ProductsController> logger)
    {
        _estoqueService = estoqueService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Product>>), 200)]
    public async Task<IActionResult> ListarProdutos([FromQuery] string? categoria = null, [FromQuery] bool apenasAtivos = true)
    {
        try
        {
            var result = await _estoqueService.ListarProdutosAsync(categoria, apenasAtivos);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar produtos");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<Product>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ObterProduto(Guid id)
    {
        try
        {
            var result = await _estoqueService.ObterProdutoPorIdAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produto {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Product>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CriarProduto([FromBody] CreateProductRequest request)
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

            var result = await _estoqueService.CriarProdutoAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(ObterProduto), new { id = result.Data!.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar produto");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Product>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AtualizarProduto(Guid id, [FromBody] UpdateProductRequest request)
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

            var result = await _estoqueService.AtualizarProdutoAsync(id, request);

            if (!result.Success)
            {
                if (result.Message?.Contains("não encontrado") == true)
                    return NotFound(result);
                
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> DeletarProduto(Guid id)
    {
        try
        {
            var result = await _estoqueService.DeletarProdutoAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar produto {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    [HttpPost("availability")]
    [ProducesResponseType(typeof(ApiResponse<ProductAvailabilityResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> VerificarDisponibilidade([FromBody] ProductAvailabilityRequest request)
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

            var result = await _estoqueService.VerificarDisponibilidadeAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar disponibilidade do produto {ProductId}", request.ProductId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }

    [HttpPost("{id:guid}/add-stock")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Product>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> AdicionarEstoque(Guid id, [FromBody] AddStockRequest request)
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

            var result = await _estoqueService.AdicionarEstoqueAsync(id, request.Quantity, request.Reason ?? "Reposição manual");

            if (!result.Success)
            {
                if (result.Message?.Contains("não encontrado") == true)
                    return NotFound(result);
                
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar estoque do produto {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Erro interno do servidor"));
        }
    }
}

public class AddStockRequest
{
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}
