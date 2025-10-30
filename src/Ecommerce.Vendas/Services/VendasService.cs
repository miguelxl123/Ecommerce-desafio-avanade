using Ecommerce.Shared.Events;
using Ecommerce.Shared.Models;
using Ecommerce.Shared.Services;
using Ecommerce.Vendas.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Ecommerce.Vendas.Services;

/// <summary>
/// Interface para serviços de gerenciamento de vendas
/// </summary>
public interface IVendasService
{
    /// <summary>
    /// Criar um novo pedido após validar disponibilidade no estoque
    /// </summary>
    Task<ApiResponse<Order>> CriarPedidoAsync(CreateOrderRequest request, Guid userId, string userEmail);
    
    /// <summary>
    /// Listar pedidos, opcionalmente filtrados por usuário
    /// </summary>
    Task<ApiResponse<IEnumerable<Order>>> ListarPedidosAsync(Guid? userId = null);
    
    /// <summary>
    /// Obter detalhes de um pedido específico
    /// </summary>
    Task<ApiResponse<Order>> ObterPedidoPorIdAsync(Guid id);
    
    /// <summary>
    /// Confirmar um pedido e publicar evento para redução de estoque
    /// </summary>
    Task<ApiResponse<Order>> ConfirmarPedidoAsync(Guid id);
    
    /// <summary>
    /// Cancelar um pedido pendente
    /// </summary>
    Task<ApiResponse<Order>> CancelarPedidoAsync(Guid id);
}

/// <summary>
/// Implementação dos serviços de gerenciamento de vendas e pedidos
/// </summary>
public class VendasService : IVendasService
{
    private readonly VendasDbContext _context;
    private readonly IRabbitMQService _rabbitMQ;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VendasService> _logger;
    private readonly IConfiguration _configuration;

    public VendasService(
        VendasDbContext context,
        IRabbitMQService rabbitMQ,
        IHttpClientFactory httpClientFactory,
        ILogger<VendasService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _rabbitMQ = rabbitMQ;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiResponse<Order>> CriarPedidoAsync(CreateOrderRequest request, Guid userId, string userEmail)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Verificar disponibilidade de todos os produtos
            var availabilityChecks = new List<Task<ProductAvailabilityResponse>>();
            
            foreach (var item in request.Items)
            {
                availabilityChecks.Add(VerificarDisponibilidadeAsync(item.ProductId, item.Quantity));
            }

            var availabilityResults = await Task.WhenAll(availabilityChecks);

            // Verificar se todos os produtos estão disponíveis
            var unavailableItems = availabilityResults.Where(r => !r.IsAvailable).ToList();
            if (unavailableItems.Any())
            {
                var errors = unavailableItems.Select(item => item.Message ?? "Produto indisponível").ToList();
                return ApiResponse<Order>.ErrorResult("Alguns produtos não estão disponíveis", errors);
            }

            // Obter informações dos produtos
            var productInfos = new Dictionary<Guid, (string name, decimal price)>();
            foreach (var item in request.Items)
            {
                var productInfo = await ObterInformacoesProdutoAsync(item.ProductId);
                if (productInfo == null)
                {
                    return ApiResponse<Order>.ErrorResult($"Produto {item.ProductId} não encontrado");
                }
                productInfos[item.ProductId] = productInfo.Value;
            }

            // Criar pedido
            var order = new Order
            {
                UserId = userId,
                UserEmail = userEmail,
                Status = OrderStatus.Pending,
                Notes = request.Notes
            };

            // Criar itens do pedido
            decimal totalAmount = 0;
            foreach (var itemRequest in request.Items)
            {
                var (productName, productPrice) = productInfos[itemRequest.ProductId];
                var totalPrice = productPrice * itemRequest.Quantity;
                
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = itemRequest.ProductId,
                    ProductName = productName,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = productPrice,
                    TotalPrice = totalPrice
                };

                order.Items.Add(orderItem);
                totalAmount += totalPrice;
            }

            order.TotalAmount = totalAmount;

            // Salvar no banco
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Pedido {OrderId} criado com sucesso para usuário {UserId}", order.Id, userId);

            return ApiResponse<Order>.SuccessResult(order, "Pedido criado com sucesso");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao criar pedido para usuário {UserId}", userId);
            return ApiResponse<Order>.ErrorResult("Erro interno ao criar pedido");
        }
    }

    public async Task<ApiResponse<Order>> ConfirmarPedidoAsync(Guid id)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return ApiResponse<Order>.ErrorResult("Pedido não encontrado");
            }

            if (order.Status != OrderStatus.Pending)
            {
                return ApiResponse<Order>.ErrorResult("Apenas pedidos pendentes podem ser confirmados");
            }

            // Verificar disponibilidade novamente antes da confirmação
            foreach (var item in order.Items)
            {
                var availability = await VerificarDisponibilidadeAsync(item.ProductId, item.Quantity);
                if (!availability.IsAvailable)
                {
                    return ApiResponse<Order>.ErrorResult($"Produto {item.ProductName} não está mais disponível na quantidade solicitada");
                }
            }

            // Confirmar pedido
            order.Status = OrderStatus.Confirmed;
            order.ConfirmedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publicar evento de venda criada
            var vendaCriadaEvent = new VendaCriadaEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                UserEmail = order.UserEmail,
                TotalAmount = order.TotalAmount,
                Items = order.Items.Select(item => new VendaItemEvent
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            };

            await _rabbitMQ.PublishAsync("vendas.exchange", "venda.criada", vendaCriadaEvent);

            _logger.LogInformation("Pedido {OrderId} confirmado e evento publicado", order.Id);

            return ApiResponse<Order>.SuccessResult(order, "Pedido confirmado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao confirmar pedido {OrderId}", id);
            return ApiResponse<Order>.ErrorResult("Erro interno ao confirmar pedido");
        }
    }

    public async Task<ApiResponse<IEnumerable<Order>>> ListarPedidosAsync(Guid? userId = null)
    {
        try
        {
            var query = _context.Orders.Include(o => o.Items).AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(o => o.UserId == userId.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return ApiResponse<IEnumerable<Order>>.SuccessResult(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar pedidos");
            return ApiResponse<IEnumerable<Order>>.ErrorResult("Erro interno ao listar pedidos");
        }
    }

    public async Task<ApiResponse<Order>> ObterPedidoPorIdAsync(Guid id)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return ApiResponse<Order>.ErrorResult("Pedido não encontrado");
            }

            return ApiResponse<Order>.SuccessResult(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pedido {OrderId}", id);
            return ApiResponse<Order>.ErrorResult("Erro interno ao obter pedido");
        }
    }

    public async Task<ApiResponse<Order>> CancelarPedidoAsync(Guid id)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return ApiResponse<Order>.ErrorResult("Pedido não encontrado");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return ApiResponse<Order>.ErrorResult("Pedido já está cancelado");
            }

            if (order.Status == OrderStatus.Delivered)
            {
                return ApiResponse<Order>.ErrorResult("Pedidos entregues não podem ser cancelados");
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Pedido {OrderId} cancelado", order.Id);

            return ApiResponse<Order>.SuccessResult(order, "Pedido cancelado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar pedido {OrderId}", id);
            return ApiResponse<Order>.ErrorResult("Erro interno ao cancelar pedido");
        }
    }

    private async Task<ProductAvailabilityResponse> VerificarDisponibilidadeAsync(Guid productId, int quantity)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var estoqueUrl = _configuration["Services:Estoque"] ?? "http://localhost:5002";
            
            var request = new ProductAvailabilityRequest
            {
                ProductId = productId,
                Quantity = quantity
            };

            var response = await httpClient.PostAsJsonAsync($"{estoqueUrl}/api/products/availability", request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductAvailabilityResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return apiResponse?.Data ?? new ProductAvailabilityResponse 
                { 
                    ProductId = productId, 
                    IsAvailable = false, 
                    Message = "Falha na verificação de disponibilidade" 
                };
            }

            return new ProductAvailabilityResponse 
            { 
                ProductId = productId, 
                IsAvailable = false, 
                Message = "Serviço de estoque indisponível" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar disponibilidade do produto {ProductId}", productId);
            return new ProductAvailabilityResponse 
            { 
                ProductId = productId, 
                IsAvailable = false, 
                Message = "Erro na comunicação com serviço de estoque" 
            };
        }
    }

    private async Task<(string name, decimal price)?> ObterInformacoesProdutoAsync(Guid productId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var estoqueUrl = _configuration["Services:Estoque"] ?? "http://localhost:5002";
            
            var response = await httpClient.GetAsync($"{estoqueUrl}/api/products/{productId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<Product>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.Data != null)
                {
                    return (apiResponse.Data.Name, apiResponse.Data.Price);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do produto {ProductId}", productId);
            return null;
        }
    }
}