using Ecommerce.Shared.Events;
using Ecommerce.Shared.Models;
using Ecommerce.Shared.Services;
using Ecommerce.Estoque.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Estoque.Services;

/// <summary>
/// Interface para serviços de gerenciamento de estoque e produtos
/// </summary>
public interface IEstoqueService
{
    /// <summary>
    /// Listar produtos do catálogo com filtros opcionais
    /// </summary>
    Task<ApiResponse<IEnumerable<Product>>> ListarProdutosAsync(string? categoria = null, bool apenasAtivos = true);
    
    /// <summary>
    /// Obter produto específico por ID
    /// </summary>
    Task<ApiResponse<Product>> ObterProdutoPorIdAsync(Guid id);
    
    /// <summary>
    /// Criar novo produto no catálogo
    /// </summary>
    Task<ApiResponse<Product>> CriarProdutoAsync(CreateProductRequest request);
    
    /// <summary>
    /// Atualizar informações de um produto existente
    /// </summary>
    Task<ApiResponse<Product>> AtualizarProdutoAsync(Guid id, UpdateProductRequest request);
    
    /// <summary>
    /// Remover produto do catálogo (soft delete)
    /// </summary>
    Task<ApiResponse<bool>> DeletarProdutoAsync(Guid id);
    
    /// <summary>
    /// Verificar disponibilidade de produto e quantidade em estoque
    /// </summary>
    Task<ApiResponse<ProductAvailabilityResponse>> VerificarDisponibilidadeAsync(ProductAvailabilityRequest request);
    
    /// <summary>
    /// Dar baixa no estoque de um produto (geralmente após venda)
    /// </summary>
    Task<ApiResponse<Product>> DarBaixaEstoqueAsync(Guid productId, int quantity, string reason = "Venda realizada");
    
    /// <summary>
    /// Adicionar produtos ao estoque (reposição)
    /// </summary>
    Task<ApiResponse<Product>> AdicionarEstoqueAsync(Guid productId, int quantity, string reason = "Reposição");
}

/// <summary>
/// Implementação dos serviços de gerenciamento de estoque e produtos
/// </summary>
public class EstoqueService : IEstoqueService
{
    private readonly EstoqueDbContext _context;
    private readonly IRabbitMQService _rabbitMQ;
    private readonly ILogger<EstoqueService> _logger;

    public EstoqueService(
        EstoqueDbContext context,
        IRabbitMQService rabbitMQ,
        ILogger<EstoqueService> logger)
    {
        _context = context;
        _rabbitMQ = rabbitMQ;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<Product>>> ListarProdutosAsync(string? categoria = null, bool apenasAtivos = true)
    {
        try
        {
            var query = _context.Products.AsQueryable();

            if (apenasAtivos)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(p => p.Category != null && p.Category.ToLower().Contains(categoria.ToLower()));
            }

            var products = await query
                .OrderBy(p => p.Name)
                .ToListAsync();

            return ApiResponse<IEnumerable<Product>>.SuccessResult(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar produtos");
            return ApiResponse<IEnumerable<Product>>.ErrorResult("Erro interno ao listar produtos");
        }
    }

    public async Task<ApiResponse<Product>> ObterProdutoPorIdAsync(Guid id)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return ApiResponse<Product>.ErrorResult("Produto não encontrado");
            }

            return ApiResponse<Product>.SuccessResult(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produto {ProductId}", id);
            return ApiResponse<Product>.ErrorResult("Erro interno ao obter produto");
        }
    }

    public async Task<ApiResponse<Product>> CriarProdutoAsync(CreateProductRequest request)
    {
        try
        {
            // Verificar se SKU já existe
            if (!string.IsNullOrEmpty(request.Sku))
            {
                var existingSku = await _context.Products
                    .AnyAsync(p => p.Sku == request.Sku);

                if (existingSku)
                {
                    return ApiResponse<Product>.ErrorResult("SKU já existe");
                }
            }

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                Category = request.Category,
                Sku = request.Sku,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Produto {ProductId} criado com sucesso", product.Id);

            return ApiResponse<Product>.SuccessResult(product, "Produto criado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar produto");
            return ApiResponse<Product>.ErrorResult("Erro interno ao criar produto");
        }
    }

    public async Task<ApiResponse<Product>> AtualizarProdutoAsync(Guid id, UpdateProductRequest request)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return ApiResponse<Product>.ErrorResult("Produto não encontrado");
            }

            // Verificar se SKU já existe (se foi alterado)
            if (!string.IsNullOrEmpty(request.Sku) && request.Sku != product.Sku)
            {
                var existingSku = await _context.Products
                    .AnyAsync(p => p.Sku == request.Sku && p.Id != id);

                if (existingSku)
                {
                    return ApiResponse<Product>.ErrorResult("SKU já existe");
                }
            }

            // Atualizar campos se fornecidos
            if (!string.IsNullOrEmpty(request.Name))
                product.Name = request.Name;

            if (request.Description != null)
                product.Description = request.Description;

            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            if (request.StockQuantity.HasValue)
                product.StockQuantity = request.StockQuantity.Value;

            if (!string.IsNullOrEmpty(request.Category))
                product.Category = request.Category;

            if (!string.IsNullOrEmpty(request.Sku))
                product.Sku = request.Sku;

            if (request.IsActive.HasValue)
                product.IsActive = request.IsActive.Value;

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Produto {ProductId} atualizado com sucesso", product.Id);

            return ApiResponse<Product>.SuccessResult(product, "Produto atualizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto {ProductId}", id);
            return ApiResponse<Product>.ErrorResult("Erro interno ao atualizar produto");
        }
    }

    public async Task<ApiResponse<bool>> DeletarProdutoAsync(Guid id)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return ApiResponse<bool>.ErrorResult("Produto não encontrado");
            }

            // Soft delete - apenas marcar como inativo
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Produto {ProductId} desativado com sucesso", product.Id);

            return ApiResponse<bool>.SuccessResult(true, "Produto desativado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar produto {ProductId}", id);
            return ApiResponse<bool>.ErrorResult("Erro interno ao deletar produto");
        }
    }

    public async Task<ApiResponse<ProductAvailabilityResponse>> VerificarDisponibilidadeAsync(ProductAvailabilityRequest request)
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);

            if (product == null)
            {
                return ApiResponse<ProductAvailabilityResponse>.SuccessResult(
                    new ProductAvailabilityResponse
                    {
                        ProductId = request.ProductId,
                        IsAvailable = false,
                        AvailableQuantity = 0,
                        Message = "Produto não encontrado ou inativo"
                    });
            }

            var isAvailable = product.StockQuantity >= request.Quantity;

            return ApiResponse<ProductAvailabilityResponse>.SuccessResult(
                new ProductAvailabilityResponse
                {
                    ProductId = request.ProductId,
                    IsAvailable = isAvailable,
                    AvailableQuantity = product.StockQuantity,
                    Message = isAvailable ? "Produto disponível" : "Quantidade insuficiente em estoque"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar disponibilidade do produto {ProductId}", request.ProductId);
            return ApiResponse<ProductAvailabilityResponse>.ErrorResult("Erro interno ao verificar disponibilidade");
        }
    }

    public async Task<ApiResponse<Product>> DarBaixaEstoqueAsync(Guid productId, int quantity, string reason = "Venda realizada")
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product == null)
            {
                return ApiResponse<Product>.ErrorResult("Produto não encontrado ou inativo");
            }

            if (product.StockQuantity < quantity)
            {
                return ApiResponse<Product>.ErrorResult("Quantidade insuficiente em estoque");
            }

            product.StockQuantity -= quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Baixa de estoque realizada: Produto {ProductId}, Quantidade {Quantity}, Motivo: {Reason}", 
                productId, quantity, reason);

            return ApiResponse<Product>.SuccessResult(product, "Baixa de estoque realizada com sucesso");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao dar baixa no estoque do produto {ProductId}", productId);
            return ApiResponse<Product>.ErrorResult("Erro interno ao dar baixa no estoque");
        }
    }

    public async Task<ApiResponse<Product>> AdicionarEstoqueAsync(Guid productId, int quantity, string reason = "Reposição")
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product == null)
            {
                return ApiResponse<Product>.ErrorResult("Produto não encontrado ou inativo");
            }

            product.StockQuantity += quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Estoque adicionado: Produto {ProductId}, Quantidade {Quantity}, Motivo: {Reason}", 
                productId, quantity, reason);

            return ApiResponse<Product>.SuccessResult(product, "Estoque adicionado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar estoque do produto {ProductId}", productId);
            return ApiResponse<Product>.ErrorResult("Erro interno ao adicionar estoque");
        }
    }
}