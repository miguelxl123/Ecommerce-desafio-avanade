# 🔍 EXPLICAÇÃO DETALHADA DO CÓDIGO

## 📂 ESTRUTURA COMPLETA DO PROJETO

```
📦 Ecommerce-desafio-avanade/
├── 📁 src/
│   ├── 📁 ApiGateway/                    # Gateway centralizando acesso
│   │   ├── Controllers/AuthController.cs # Autenticação JWT
│   │   ├── Services/AuthService.cs      # Lógica de autenticação
│   │   ├── Program.cs                   # Configuração YARP + JWT
│   │   └── appsettings.json            # Roteamento e configurações
│   │
│   ├── 📁 Ecommerce.Vendas/             # Microserviço de Vendas
│   │   ├── Controllers/OrdersController.cs # Endpoints de pedidos
│   │   ├── Services/VendasService.cs    # Lógica de negócio vendas
│   │   ├── Data/VendasDbContext.cs      # Contexto EF vendas
│   │   └── Program.cs                   # Configuração do serviço
│   │
│   ├── 📁 Ecommerce.Estoque/            # Microserviço de Estoque
│   │   ├── Controllers/ProductsController.cs # Endpoints produtos
│   │   ├── Services/EstoqueService.cs   # Lógica de negócio estoque
│   │   ├── Services/RabbitMQConsumerService.cs # Consumer background
│   │   ├── Data/EstoqueDbContext.cs     # Contexto EF estoque
│   │   └── Program.cs                   # Configuração do serviço
│   │
│   └── 📁 Ecommerce.Shared/             # Biblioteca compartilhada
│       ├── Models/Entities.cs          # Entidades de domínio
│       ├── Models/Requests.cs          # DTOs e requests
│       ├── Events/Events.cs            # Eventos RabbitMQ
│       └── Services/RabbitMQService.cs # Serviço de mensageria
│
├── 📁 database/                        # Scripts SQL inicialização
├── 📄 docker-compose.yml              # Orquestração completa
├── 📄 test-quick.ps1                  # Teste automatizado
└── 📄 postman-collection.json        # Collection Postman
```

---

## 🚪 **API GATEWAY - EXPLICAÇÃO DETALHADA**

### **AuthController.cs - Como Funciona:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    // Injeção de dependências - padrão DI do .NET
    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    /// <summary>
    /// Login do usuário gerando token JWT
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // 1. Validar dados de entrada
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Dados inválidos"));

            // 2. Validar credenciais do usuário
            var user = await _authService.ValidateUserAsync(request.Email, request.Password);
            
            if (user == null)
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("Credenciais inválidas"));

            // 3. Gerar token JWT com claims do usuário
            var token = GenerateJwtToken(user);

            // 4. Retornar resposta com token
            var response = new LoginResponse
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login realizado com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Gera token JWT com claims do usuário
    /// </summary>
    private string GenerateJwtToken(UserModel user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

        // Claims são informações incluídas no token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Configurar token JWT
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1), // Token válido por 1 hora
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

### **Program.cs - Configuração YARP:**

```csharp
// Configurar YARP para roteamento
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Pipeline do YARP
app.MapReverseProxy(); // Deve ser o último no pipeline
```

### **appsettings.json - Roteamento:**

```json
{
  "ReverseProxy": {
    "Routes": {
      "vendas-route": {
        "ClusterId": "vendas-cluster",
        "Match": {
          "Path": "/api/vendas/{**catch-all}"  // {**catch-all} captura toda URL
        }
      },
      "inventario-route": {
        "ClusterId": "inventario-cluster", 
        "Match": {
          "Path": "/api/inventario/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "vendas-cluster": {
        "Destinations": {
          "vendas-destination": {
            "Address": "http://microservico-vendas:5001/"  // Nome do container Docker
          }
        }
      },
      "inventario-cluster": {
        "Destinations": {
          "inventario-destination": {
            "Address": "http://microservico-estoque:5002/"
          }
        }
      }
    }
  }
}
```

---

## 🛒 **MICROSERVIÇO VENDAS - EXPLICAÇÃO DETALHADA**

### **VendasService.cs - Lógica de Negócio:**

```csharp
public class VendasService : IVendasService
{
    private readonly VendasDbContext _context;
    private readonly IRabbitMQService _rabbitMQ;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VendasService> _logger;

    /// <summary>
    /// Criar pedido com validação completa
    /// </summary>
    public async Task<ApiResponse<Order>> CriarPedidoAsync(CreateOrderRequest request, Guid userId, string userEmail)
    {
        try
        {
            // 1. VALIDAR DISPONIBILIDADE DE TODOS OS PRODUTOS
            var unavailableItems = new List<object>();
            var productDetails = new List<(Guid ProductId, string Name, decimal Price)>();

            foreach (var item in request.Items)
            {
                // Verificar se produto existe e tem estoque
                var availability = await VerificarDisponibilidadeProdutoAsync(item.ProductId, item.Quantity);
                
                if (!availability.IsAvailable)
                {
                    unavailableItems.Add(new 
                    { 
                        ProductId = item.ProductId, 
                        Message = availability.Message ?? "Produto indisponível" 
                    });
                    continue;
                }

                // Buscar detalhes do produto para calcular preços
                var product = await ObterDetalhesProdutoAsync(item.ProductId);
                if (product == null)
                {
                    return ApiResponse<Order>.ErrorResult($"Produto {item.ProductId} não encontrado");
                }

                productDetails.Add((item.ProductId, product.Name, product.Price));
            }

            // Se algum produto não está disponível, falhar
            if (unavailableItems.Any())
            {
                var errors = unavailableItems.Select(item => item.ToString()).ToList();
                return ApiResponse<Order>.ErrorResult("Alguns produtos não estão disponíveis", errors);
            }

            // 2. CRIAR PEDIDO NO BANCO
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserEmail = userEmail,
                Status = OrderStatus.Pending, // Começa como Pendente
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };

            // 3. ADICIONAR ITENS DO PEDIDO
            decimal totalAmount = 0;
            foreach (var item in request.Items)
            {
                var productDetail = productDetails.First(p => p.ProductId == item.ProductId);
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = productDetail.Name,
                    Quantity = item.Quantity,
                    UnitPrice = productDetail.Price,
                    TotalPrice = productDetail.Price * item.Quantity
                };

                order.Items.Add(orderItem);
                totalAmount += orderItem.TotalPrice;
            }

            order.TotalAmount = totalAmount;

            // 4. SALVAR NO BANCO DE DADOS
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pedido {OrderId} criado com sucesso para usuário {UserId}", order.Id, userId);

            return ApiResponse<Order>.SuccessResult(order, "Pedido criado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar pedido para usuário {UserId}", userId);
            return ApiResponse<Order>.ErrorResult("Erro interno ao criar pedido");
        }
    }

    /// <summary>
    /// Confirmar pedido e publicar evento no RabbitMQ
    /// </summary>
    public async Task<ApiResponse<Order>> ConfirmarPedidoAsync(Guid id)
    {
        try
        {
            // 1. BUSCAR PEDIDO
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return ApiResponse<Order>.ErrorResult("Pedido não encontrado");

            // 2. VALIDAR STATUS
            if (order.Status != OrderStatus.Pending)
                return ApiResponse<Order>.ErrorResult("Apenas pedidos pendentes podem ser confirmados");

            // 3. REVALIDAR ESTOQUE (podem ter outras vendas entre criação e confirmação)
            foreach (var item in order.Items)
            {
                var availability = await VerificarDisponibilidadeProdutoAsync(item.ProductId, item.Quantity);
                if (!availability.IsAvailable)
                {
                    return ApiResponse<Order>.ErrorResult($"Produto {item.ProductName} não está mais disponível na quantidade solicitada");
                }
            }

            // 4. CONFIRMAR PEDIDO
            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.UtcNow;

            // 5. PUBLICAR EVENTO NO RABBITMQ
            var vendaEvent = new VendaCriadaEvent
            {
                PedidoId = order.Id,
                Items = order.Items.Select(i => new VendaItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList(),
                DataVenda = DateTime.UtcNow
            };

            // Publicar na exchange 'venda' com routing key 'criada'
            await _rabbitMQ.PublishAsync("venda", "criada", vendaEvent);

            // 6. SALVAR MUDANÇAS
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pedido {OrderId} confirmado e evento publicado", order.Id);

            return ApiResponse<Order>.SuccessResult(order, "Pedido confirmado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao confirmar pedido {OrderId}", id);
            return ApiResponse<Order>.ErrorResult("Erro interno ao confirmar pedido");
        }
    }

    /// <summary>
    /// Verificar disponibilidade via HTTP com microserviço de estoque
    /// </summary>
    private async Task<ProductAvailabilityResponse> VerificarDisponibilidadeProdutoAsync(Guid productId, int quantity)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var estoqueServiceUrl = _configuration.GetValue<string>("Services:EstoqueService:BaseUrl");

            var request = new ProductAvailabilityRequest
            {
                ProductId = productId,
                Quantity = quantity
            };

            // Chamada HTTP para microserviço de estoque
            var response = await httpClient.PostAsJsonAsync(
                $"{estoqueServiceUrl}/api/products/availability", 
                request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductAvailabilityResponse>>(json);
                return apiResponse?.Data ?? new ProductAvailabilityResponse { IsAvailable = false };
            }

            return new ProductAvailabilityResponse { IsAvailable = false, Message = "Erro ao verificar estoque" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar disponibilidade do produto {ProductId}", productId);
            return new ProductAvailabilityResponse { IsAvailable = false, Message = "Erro interno" };
        }
    }
}
```

---

## 📦 **MICROSERVIÇO ESTOQUE - EXPLICAÇÃO DETALHADA**

### **RabbitMQConsumerService.cs - Background Service:**

```csharp
/// <summary>
/// Serviço que roda em background consumindo mensagens do RabbitMQ
/// </summary>
public class RabbitMQConsumerService : BackgroundService, IRabbitMQConsumerService
{
    private readonly IRabbitMQService _rabbitMQ;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQConsumerService> _logger;

    public RabbitMQConsumerService(
        IRabbitMQService rabbitMQ,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQConsumerService> logger)
    {
        _rabbitMQ = rabbitMQ;
        _serviceProvider = serviceProvider; // Para criar scopes
        _logger = logger;
    }

    /// <summary>
    /// Método principal executado em background
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Configurar consumo de mensagens da fila 'venda.criada'
            await _rabbitMQ.SubscribeAsync<VendaCriadaEvent>("venda.criada", ProcessarVendaCriada);
            
            _logger.LogInformation("RabbitMQ Consumer Service iniciado e aguardando mensagens");

            // Manter o serviço rodando
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no RabbitMQ Consumer Service");
        }
    }

    /// <summary>
    /// Processar evento de venda criada - DAR BAIXA NO ESTOQUE
    /// </summary>
    private async Task ProcessarVendaCriada(VendaCriadaEvent vendaEvent)
    {
        // Criar scope porque este é um singleton e precisa acessar serviços scoped
        using var scope = _serviceProvider.CreateScope();
        var estoqueService = scope.ServiceProvider.GetRequiredService<IEstoqueService>();

        try
        {
            _logger.LogInformation("Processando venda {PedidoId} com {ItemCount} itens", 
                vendaEvent.PedidoId, vendaEvent.Items.Count);

            // Processar cada item da venda
            foreach (var item in vendaEvent.Items)
            {
                // Dar baixa no estoque
                var result = await estoqueService.DarBaixaEstoqueAsync(
                    item.ProductId, 
                    item.Quantity, 
                    $"Venda pedido {vendaEvent.PedidoId}");

                if (result.Success)
                {
                    _logger.LogInformation("Baixa de estoque realizada: Produto {ProductId}, Quantidade {Quantity}", 
                        item.ProductId, item.Quantity);
                }
                else
                {
                    _logger.LogWarning("Falha na baixa de estoque: Produto {ProductId}, Erro: {Error}", 
                        item.ProductId, result.Message);
                }
            }

            _logger.LogInformation("Processamento da venda {PedidoId} concluído", vendaEvent.PedidoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar venda {PedidoId}", vendaEvent.PedidoId);
            // Em produção, aqui você poderia implementar retry logic ou dead letter queue
            throw; // RabbitMQ vai rejeitar a mensagem e tentar novamente
        }
    }
}
```

### **EstoqueService.cs - Baixa de Estoque:**

```csharp
/// <summary>
/// Dar baixa no estoque após venda confirmada
/// </summary>
public async Task<ApiResponse<Product>> DarBaixaEstoqueAsync(Guid productId, int quantity, string reason = "Venda realizada")
{
    try
    {
        // 1. BUSCAR PRODUTO
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        
        if (product == null)
            return ApiResponse<Product>.ErrorResult("Produto não encontrado");

        // 2. VERIFICAR SE TEM ESTOQUE SUFICIENTE
        if (product.StockQuantity < quantity)
        {
            _logger.LogWarning("Tentativa de baixa maior que estoque disponível. Produto: {ProductId}, Estoque: {Stock}, Solicitado: {Quantity}", 
                productId, product.StockQuantity, quantity);
            
            return ApiResponse<Product>.ErrorResult($"Estoque insuficiente. Disponível: {product.StockQuantity}, Solicitado: {quantity}");
        }

        // 3. REDUZIR ESTOQUE
        var estoqueAnterior = product.StockQuantity;
        product.StockQuantity -= quantity;
        product.UpdatedAt = DateTime.UtcNow;

        // 4. SALVAR ALTERAÇÃO
        await _context.SaveChangesAsync();

        _logger.LogInformation("Baixa de estoque realizada: Produto {ProductId} ({ProductName}), Estoque anterior: {EstoqueAnterior}, Baixa: {Quantity}, Estoque atual: {EstoqueAtual}, Motivo: {Reason}", 
            product.Id, product.Name, estoqueAnterior, quantity, product.StockQuantity, reason);

        // 5. PUBLICAR EVENTO DE ESTOQUE ATUALIZADO (opcional)
        var estoqueEvent = new EstoqueAtualizadoEvent
        {
            ProductId = product.Id,
            QuantidadeAnterior = estoqueAnterior,
            QuantidadeAtual = product.StockQuantity,
            TipoMovimentacao = "SAIDA",
            Motivo = reason,
            DataMovimentacao = DateTime.UtcNow
        };

        await _rabbitMQ.PublishAsync("estoque", "atualizado", estoqueEvent);

        return ApiResponse<Product>.SuccessResult(product, "Baixa de estoque realizada com sucesso");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao dar baixa no estoque do produto {ProductId}", productId);
        return ApiResponse<Product>.ErrorResult("Erro interno ao atualizar estoque");
    }
}
```

---

## 🐰 **RABBITMQ SERVICE - EXPLICAÇÃO DETALHADA**

### **RabbitMQService.cs - Publisher e Consumer:**

```csharp
public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        var connectionString = configuration.GetConnectionString("RabbitMQ") ?? "amqp://localhost:5672";
        
        try
        {
            // Configurar factory de conexão
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(connectionString),
                DispatchConsumersAsync = true // Importante para consumers async
            };
            
            // Criar conexão e canal
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("Conectado ao RabbitMQ em {ConnectionString}", connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao conectar ao RabbitMQ em {ConnectionString}", connectionString);
            throw;
        }
    }

    /// <summary>
    /// Publicar mensagem em exchange/routing key
    /// </summary>
    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        try
        {
            // 1. DECLARAR EXCHANGE (Topic permite routing flexível)
            _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

            // 2. SERIALIZAR MENSAGEM
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var body = Encoding.UTF8.GetBytes(json);

            // 3. CONFIGURAR PROPRIEDADES DA MENSAGEM
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Mensagem sobrevive a restart do broker
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2; // Persistent

            // 4. PUBLICAR MENSAGEM
            _channel.BasicPublish(exchange, routingKey, properties, body);
            
            _logger.LogInformation("Mensagem publicada na exchange {Exchange} com routing key {RoutingKey}", 
                exchange, routingKey);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar mensagem na exchange {Exchange} com routing key {RoutingKey}", 
                exchange, routingKey);
            throw;
        }
    }

    /// <summary>
    /// Subscrever a uma fila para consumir mensagens
    /// </summary>
    public async Task SubscribeAsync<T>(string queue, Func<T, Task> handler)
    {
        try
        {
            // 1. DECLARAR FILA
            _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);

            // 2. CONFIGURAR QoS (Quality of Service)
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false); // Processar uma mensagem por vez

            // 3. CRIAR CONSUMER ASSÍNCRONO
            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            // 4. CONFIGURAR HANDLER DE MENSAGENS
            consumer.Received += async (model, eventArgs) =>
            {
                try
                {
                    // Deserializar mensagem
                    var body = eventArgs.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    });

                    if (message != null)
                    {
                        // Processar mensagem
                        await handler(message);
                        
                        // ACK (confirmar processamento)
                        _channel.BasicAck(eventArgs.DeliveryTag, false);
                        
                        _logger.LogInformation("Mensagem processada com sucesso da fila {Queue}", queue);
                    }
                    else
                    {
                        _logger.LogWarning("Mensagem nula recebida da fila {Queue}", queue);
                        
                        // NACK (rejeitar mensagem)
                        _channel.BasicNack(eventArgs.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem da fila {Queue}", queue);
                    
                    // NACK com requeue (tentar novamente)
                    _channel.BasicNack(eventArgs.DeliveryTag, false, true);
                }
            };

            // 5. INICIAR CONSUMO
            _channel.BasicConsume(queue, false, consumer); // autoAck: false para controle manual
            
            _logger.LogInformation("Iniciado consumo de mensagens da fila {Queue}", queue);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao subscrever na fila {Queue}", queue);
            throw;
        }
    }
}
```

---

## 🔒 **SEGURANÇA E AUTORIZAÇÃO**

### **Como Claims Funcionam:**

```csharp
// No JWT são incluídas estas informações (Claims)
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID do usuário
    new Claim(ClaimTypes.Email, user.Email),                  // Email
    new Claim(ClaimTypes.Name, user.Name),                    // Nome
    new Claim(ClaimTypes.Role, user.Role),                    // Role (Admin/User)
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Token ID único
};

// Nos controllers, acessamos assim:
private Guid GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
}

private string GetUserRoleFromClaims()
{
    return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
}

// Autorização por role
[Authorize(Roles = "Admin")] // Só admins podem acessar
public async Task<IActionResult> CriarProduto([FromBody] CreateProductRequest request)

// Verificação programática
if (GetUserRoleFromClaims() != "Admin")
{
    return Forbid(); // HTTP 403
}

// Verificação de ownership (usuário só vê seus próprios pedidos)
if (userRole != "Admin" && order.UserId != userId)
{
    return Forbid();
}
```

---

## 🗄️ **ENTITY FRAMEWORK E BANCO**

### **DbContext Configuration:**

```csharp
public class VendasDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public VendasDbContext(DbContextOptions<VendasDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurar relacionamentos
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2); // Para valores monetários
            
            // Relacionamento um-para-muitos
            entity.HasMany(o => o.Items)
                  .WithOne()
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade); // Deleta itens quando deleta pedido
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
        });

        // Seed data (dados iniciais)
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Dados de exemplo para desenvolvimento
        var products = new[]
        {
            new Product 
            { 
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Samsung Galaxy S23",
                Description = "Smartphone premium",
                Price = 2999.99m,
                StockQuantity = 50,
                Category = "Smartphones",
                SKU = "SGS23-001",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
            // ... mais produtos
        };

        modelBuilder.Entity<Product>().HasData(products);
    }
}
```

### **Migrations:**

```csharp
// Comandos para gerenciar migrations
// dotnet ef migrations add InitialCreate --project src/Ecommerce.Vendas
// dotnet ef database update --project src/Ecommerce.Vendas

// Migração automática em desenvolvimento
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<VendasDbContext>();
    
    try
    {
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Migração do banco de dados concluída com sucesso");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erro ao realizar migração do banco de dados");
    }
}
```

---

## 🐳 **DOCKER E CONTAINERIZAÇÃO**

### **Dockerfile Exemplo:**

```dockerfile
# Dockerfile para microserviço
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto e restaurar dependências
COPY ["src/Ecommerce.Vendas/Ecommerce.Vendas.csproj", "src/Ecommerce.Vendas/"]
COPY ["src/Ecommerce.Shared/Ecommerce.Shared.csproj", "src/Ecommerce.Shared/"]
RUN dotnet restore "src/Ecommerce.Vendas/Ecommerce.Vendas.csproj"

# Copiar código fonte e compilar
COPY . .
WORKDIR "/src/src/Ecommerce.Vendas"
RUN dotnet build "Ecommerce.Vendas.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ecommerce.Vendas.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagem final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.Vendas.dll"]
```

### **Docker Compose Explicado:**

```yaml
version: '3.8'

services:
  # API Gateway
  api-gateway:
    build: 
      context: .
      dockerfile: src/ApiGateway/Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5000
    depends_on:
      - postgres
      - rabbitmq
    networks:
      - ecommerce-network

  # Microserviço de Vendas  
  microservico-vendas:
    build:
      context: .
      dockerfile: src/Ecommerce.Vendas/Dockerfile
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5001
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=ecommerce;Username=postgres;Password=postgres123;Schema=vendasdb
      - ConnectionStrings__RabbitMQ=amqp://admin:admin123@rabbitmq:5672/
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    networks:
      - ecommerce-network

  # PostgreSQL
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: ecommerce
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./database:/docker-entrypoint-initdb.d/  # Scripts de inicialização
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - ecommerce-network

  # RabbitMQ
  rabbitmq:
    image: rabbitmq:3-management-alpine
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin123
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - ecommerce-network

volumes:
  postgres_data:
  rabbitmq_data:

networks:
  ecommerce-network:
    driver: bridge
```

---

## 🧪 **TESTES E VALIDAÇÃO**

### **Teste Automatizado PowerShell:**

```powershell
# test-quick.ps1 - Explicação do funcionamento

Write-Host "=== TESTE ECOMMERCE MICROSERVICES ===" -ForegroundColor Green

# 1. HEALTH CHECK - Verificar se serviços estão rodando
Write-Host "1. Health Check..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get
    if ($healthResponse.Status -eq "Saudável") {
        Write-Host "OK" -ForegroundColor Green
    }
} catch {
    Write-Host "FALHA - Serviços não estão rodando" -ForegroundColor Red
    exit 1
}

# 2. LOGIN - Obter token JWT
Write-Host "2. Login..." -ForegroundColor Yellow
$loginData = @{
    email = "admin@ecommerce.com"
    password = "admin123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -Body $loginData -ContentType "application/json"
    $token = $loginResponse.data.token
    Write-Host "OK - Token obtido" -ForegroundColor Green
} catch {
    Write-Host "FALHA - Erro no login" -ForegroundColor Red
    exit 1
}

# 3. LISTAR PRODUTOS
Write-Host "3. Listar Produtos..." -ForegroundColor Yellow
$headers = @{ Authorization = "Bearer $token" }
try {
    $productsResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/inventario/products" -Method Get -Headers $headers
    $productCount = $productsResponse.data.Count
    Write-Host "OK - $productCount produtos encontrados" -ForegroundColor Green
} catch {
    Write-Host "FALHA - Erro ao listar produtos" -ForegroundColor Red
    exit 1
}

# 4. CRIAR PEDIDO
Write-Host "4. Criar Pedido..." -ForegroundColor Yellow
$orderData = @{
    items = @(
        @{
            productId = "11111111-1111-1111-1111-111111111111"
            quantity = 1
        }
    )
    notes = "Pedido de teste automatizado"
} | ConvertTo-Json -Depth 3

try {
    $orderResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/vendas/orders" -Method Post -Body $orderData -ContentType "application/json" -Headers $headers
    $orderId = $orderResponse.data.id
    Write-Host "OK - Pedido $orderId criado" -ForegroundColor Green
} catch {
    Write-Host "FALHA - Erro ao criar pedido: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 5. CONFIRMAR PEDIDO
Write-Host "5. Confirmar Pedido..." -ForegroundColor Yellow
try {
    $confirmResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/vendas/orders/$orderId/confirm" -Method Post -Headers $headers
    Write-Host "OK - Pedido confirmado" -ForegroundColor Green
} catch {
    Write-Host "FALHA - Erro ao confirmar pedido" -ForegroundColor Red
    exit 1
}

Write-Host "=== TESTES CONCLUIDOS ===" -ForegroundColor Green
Write-Host "Sistema funcionando corretamente!" -ForegroundColor Green
```

---

## 📊 **MONITORAMENTO E OBSERVABILIDADE**

### **Logs Estruturados:**

```csharp
// Configuração de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Logs com contexto estruturado
_logger.LogInformation("Pedido {OrderId} criado para usuário {UserId} com {ItemCount} itens, valor total {TotalAmount:C}", 
    order.Id, userId, order.Items.Count, order.TotalAmount);

// Logs de erro com exception
_logger.LogError(ex, "Erro ao processar venda {PedidoId} - Produto {ProductId}", 
    vendaEvent.PedidoId, item.ProductId);

// Logs de performance
using (_logger.BeginScope("ProcessarVenda-{PedidoId}", vendaEvent.PedidoId))
{
    var stopwatch = Stopwatch.StartNew();
    
    // Processar...
    
    _logger.LogInformation("Venda processada em {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
}
```

### **Health Checks:**

```csharp
// Configurar health checks
builder.Services.AddHealthChecks()
    .AddDbContext<VendasDbContext>()
    .AddRabbitMQ(builder.Configuration.GetConnectionString("RabbitMQ"));

// Endpoint de health check
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration.TotalMilliseconds
            }),
            Timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
});
```

---

**Este guia cobre todos os aspectos técnicos do projeto para sua apresentação. Use-o como referência durante a entrevista! 🚀**