# 🎯 GUIA DE APRESENTAÇÃO TÉCNICA - Ecommerce Microservices

## 📋 ROTEIRO PARA ENTREVISTA TÉCNICA

### 🎬 **INTRODUÇÃO DO PROJETO (2-3 minutos)**

**"Desenvolvi um sistema completo de ecommerce baseado em arquitetura de microserviços usando .NET Core. O projeto demonstra conhecimentos avançados em:**
- Arquitetura de microserviços
- Comunicação assíncrona via RabbitMQ
- Autenticação JWT
- API Gateway com YARP
- Containerização com Docker
- Padrões de design e boas práticas

**O sistema simula um ecommerce real com gestão de produtos, vendas e estoque integrados."**

---

## 🏗️ **ARQUITETURA GERAL (5 minutos)**

### **Visão Macro:**
```
🌐 Cliente/Frontend
     ⬇️
🚪 API Gateway (Porta 5000)
     ⬇️ Roteamento
🔀 Microserviços:
   📦 Vendas (5001)    📋 Estoque (5002)
     ⬇️                    ⬇️
🐰 RabbitMQ (Mensageria)
     ⬇️
🗄️ PostgreSQL (Banco)
```

### **Pontos-Chave para Explicar:**
1. **Separação de Responsabilidades:** Cada serviço tem uma responsabilidade específica
2. **Comunicação Assíncrona:** RabbitMQ para eventos entre serviços
3. **Ponto de Entrada Único:** API Gateway centraliza o acesso
4. **Escalabilidade:** Cada microserviço pode escalar independentemente
5. **Resiliência:** Falha em um serviço não derruba o sistema todo

---

## 🚪 **1. API GATEWAY (5 minutos)**

### **Localização:** `src/ApiGateway/`

### **Responsabilidades:**
- **Roteamento:** Direciona requisições para microserviços corretos
- **Autenticação:** Centraliza autenticação JWT
- **Documentação:** Swagger centralizado
- **CORS:** Configuração de políticas

### **Tecnologias:**
- **YARP (Yet Another Reverse Proxy)**
- **JWT Bearer Authentication**
- **Swagger/OpenAPI**

### **Código-Chave para Mostrar:**

#### **Program.cs - Configuração JWT:**
```csharp
// Configurar autenticação JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

#### **appsettings.json - Roteamento YARP:**
```json
"ReverseProxy": {
  "Routes": {
    "vendas-route": {
      "ClusterId": "vendas-cluster",
      "Match": { "Path": "/api/vendas/{**catch-all}" }
    },
    "inventario-route": {
      "ClusterId": "inventario-cluster", 
      "Match": { "Path": "/api/inventario/{**catch-all}" }
    }
  }
}
```

### **Demonstrar:**
- Login gerando token JWT
- Roteamento funcionando via gateway
- Swagger com autenticação configurada

---

## 🛒 **2. MICROSERVIÇO DE VENDAS (7 minutos)**

### **Localização:** `src/Ecommerce.Vendas/`

### **Responsabilidades:**
- Criar e gerenciar pedidos
- Validar estoque antes da venda
- Publicar eventos de venda via RabbitMQ
- Confirmar/cancelar pedidos

### **Fluxo de Negócio:**
1. **Cliente cria pedido** → Valida produtos
2. **Verifica estoque** → Integração HTTP com microserviço de estoque
3. **Cria pedido** → Status "Pendente"
4. **Confirma pedido** → Publica evento no RabbitMQ
5. **Estoque atualizado** → Automaticamente via mensageria

### **Código-Chave para Mostrar:**

#### **OrdersController.cs - Criar Pedido:**
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> CriarPedido([FromBody] CreateOrderRequest request)
{
    var userId = GetUserIdFromClaims();
    var userEmail = GetUserEmailFromClaims();
    
    var result = await _vendasService.CriarPedidoAsync(request, userId, userEmail);
    return result.Success ? Ok(result) : BadRequest(result);
}
```

#### **VendasService.cs - Validação de Estoque:**
```csharp
// Verificar disponibilidade de cada item
foreach (var item in request.Items)
{
    var availability = await VerificarDisponibilidadeProdutoAsync(item.ProductId, item.Quantity);
    if (!availability.IsAvailable)
    {
        unavailableItems.Add(new { item.ProductId, Message = availability.Message });
    }
}

if (unavailableItems.Any())
{
    return ApiResponse<Order>.ErrorResult("Alguns produtos não estão disponíveis");
}
```

#### **Publicação de Evento RabbitMQ:**
```csharp
// Publicar evento quando pedido é confirmado
await _rabbitMQ.PublishAsync("venda.criada", new VendaCriadaEvent
{
    PedidoId = pedido.Id,
    Items = pedido.Items.Select(i => new VendaItemEvent
    {
        ProductId = i.ProductId,
        Quantity = i.Quantity
    }).ToList(),
    DataVenda = DateTime.UtcNow
});
```

### **Demonstrar:**
- Criar pedido com validação
- Confirmar pedido
- Logs do evento RabbitMQ

---

## 📦 **3. MICROSERVIÇO DE ESTOQUE (7 minutos)**

### **Localização:** `src/Ecommerce.Estoque/`

### **Responsabilidades:**
- Gerenciar catálogo de produtos
- Controlar quantidades em estoque
- Consumir eventos de venda
- Baixa automática de estoque

### **Fluxo de Integração:**
1. **Vendas valida estoque** → Via API HTTP
2. **Venda confirmada** → Evento RabbitMQ
3. **Estoque consome evento** → Background service
4. **Baixa automática** → Reduz quantidade

### **Código-Chave para Mostrar:**

#### **ProductsController.cs - CRUD Produtos:**
```csharp
[HttpGet]
public async Task<IActionResult> ListarProdutos([FromQuery] string? categoria = null)
{
    var result = await _estoqueService.ListarProdutosAsync(categoria);
    return Ok(result);
}

[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> CriarProduto([FromBody] CreateProductRequest request)
{
    var result = await _estoqueService.CriarProdutoAsync(request);
    return result.Success ? Ok(result) : BadRequest(result);
}
```

#### **RabbitMQConsumerService.cs - Consumer em Background:**
```csharp
public class RabbitMQConsumerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Configurar consumo de mensagens de venda criada
        await _rabbitMQ.SubscribeAsync<VendaCriadaEvent>("venda.criada", ProcessarVendaCriada);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessarVendaCriada(VendaCriadaEvent vendaEvent)
    {
        foreach (var item in vendaEvent.Items)
        {
            await _estoqueService.DarBaixaEstoqueAsync(item.ProductId, item.Quantity);
        }
    }
}
```

### **Demonstrar:**
- Listar produtos
- Baixa automática após venda
- Logs do consumer RabbitMQ

---

## 🐰 **4. RABBITMQ - MENSAGERIA (5 minutos)**

### **Localização:** `src/Ecommerce.Shared/Services/RabbitMQService.cs`

### **Responsabilidade:**
- Comunicação assíncrona entre microserviços
- Garantir entrega de mensagens
- Desacoplamento entre serviços

### **Código-Chave para Mostrar:**

#### **RabbitMQService.cs - Publisher:**
```csharp
public async Task PublishAsync<T>(string exchange, string routingKey, T message)
{
    // Declarar exchange se não existir
    _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

    var json = JsonSerializer.Serialize(message);
    var body = Encoding.UTF8.GetBytes(json);

    var properties = _channel.CreateBasicProperties();
    properties.Persistent = true; // Garantir durabilidade
    
    _channel.BasicPublish(exchange, routingKey, properties, body);
}
```

#### **Eventos Definidos:**
```csharp
public class VendaCriadaEvent
{
    public Guid PedidoId { get; set; }
    public List<VendaItemEvent> Items { get; set; } = new();
    public DateTime DataVenda { get; set; }
}

public class VendaItemEvent  
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
```

### **Demonstrar:**
- Interface RabbitMQ Management
- Filas e exchanges configuradas
- Mensagens sendo processadas

---

## 🗄️ **5. BANCO DE DADOS (3 minutos)**

### **Estratégia:**
- **PostgreSQL** com schemas separados
- **Database per Service** pattern
- **Entity Framework Core** com migrations

### **Schemas:**
- `vendasdb` - Dados do microserviço de vendas
- `inventario` - Dados do microserviço de estoque

### **Código-Chave:**

#### **VendasDbContext.cs:**
```csharp
public class VendasDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }
}
```

#### **Migrations Automáticas:**
```csharp
// Auto-migração em desenvolvimento
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<VendasDbContext>();
    await context.Database.MigrateAsync();
}
```

---

## 🔐 **6. SEGURANÇA E AUTENTICAÇÃO (4 minutos)**

### **JWT Bearer Authentication:**
- Tokens com claims de usuário e role
- Validação em todos os microserviços
- Autorização por roles (Admin/User)

### **Código-Chave:**

#### **AuthController.cs - Login:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // Validar credenciais
    var user = await _authService.ValidateUserAsync(request.Email, request.Password);
    
    if (user == null)
        return Unauthorized();
    
    // Gerar token JWT
    var token = GenerateJwtToken(user);
    
    return Ok(new { token, user.Email, user.Name, user.Role });
}
```

#### **Autorização por Roles:**
```csharp
[HttpPost]
[Authorize(Roles = "Admin")] // Apenas admin pode criar produtos
public async Task<IActionResult> CriarProduto([FromBody] CreateProductRequest request)
```

---

## 🐳 **7. CONTAINERIZAÇÃO (3 minutos)**

### **Docker Compose:**
- Orquestração completa
- Banco PostgreSQL
- RabbitMQ com management
- Todos os microserviços

### **docker-compose.yml:**
```yaml
services:
  api-gateway:
    build: ./src/ApiGateway
    ports: ["5000:5000"]
    
  microservico-vendas:
    build: ./src/Ecommerce.Vendas  
    ports: ["5001:5001"]
    
  microservico-estoque:
    build: ./src/Ecommerce.Estoque
    ports: ["5002:5002"]
    
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: ecommerce
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
      
  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports: ["5672:5672", "15672:15672"]
```

---

## ⚡ **8. DEMONSTRAÇÃO PRÁTICA (10 minutos)**

### **Roteiro de Demo:**

1. **Iniciar Sistema:**
   ```bash
   docker-compose up -d
   ```

2. **Teste Automatizado:**
   ```powershell
   .\test-quick.ps1
   ```

3. **Demonstração Manual:**
   - Swagger UI: http://localhost:5000/swagger
   - Login → Obter token
   - Listar produtos
   - Criar pedido
   - Confirmar pedido
   - Verificar estoque reduzido

4. **Monitoramento:**
   - RabbitMQ Management: http://localhost:15672
   - Logs em tempo real
   - Health checks

---

## 🎯 **PONTOS TÉCNICOS AVANÇADOS PARA DESTACAR**

### **Padrões de Design Implementados:**
- ✅ **Repository Pattern** (via Entity Framework)
- ✅ **Service Layer Pattern** (separação de responsabilidades)
- ✅ **Gateway Pattern** (API Gateway com YARP)
- ✅ **Observer Pattern** (RabbitMQ events)
- ✅ **Background Service Pattern** (RabbitMQ consumer)

### **Princípios SOLID:**
- ✅ **Single Responsibility** - Cada microserviço uma responsabilidade
- ✅ **Open/Closed** - Extensível via interfaces
- ✅ **Liskov Substitution** - Interfaces bem definidas
- ✅ **Interface Segregation** - Interfaces específicas
- ✅ **Dependency Inversion** - Injeção de dependência

### **Características de Microserviços:**
- ✅ **Database per Service**
- ✅ **API Gateway Pattern**
- ✅ **Event-Driven Architecture**
- ✅ **Service Discovery** (via Docker)
- ✅ **Circuit Breaker** (tratamento de falhas)

### **Qualidade do Código:**
- ✅ **Documentação XML** completa
- ✅ **Logs estruturados** em português
- ✅ **Validation** com Data Annotations
- ✅ **Exception Handling** centralizado
- ✅ **Configuration** via appsettings

---

## 💡 **PERGUNTAS POSSÍVEIS E RESPOSTAS**

### **"Por que microserviços e não monolito?"**
**R:** "Escolhi microserviços para demonstrar conhecimento em arquiteturas distribuídas. Cada serviço pode escalar independentemente, usar tecnologias diferentes se necessário, e falhas são isoladas. Para um projeto real, dependeria do contexto - team size, complexidade do domínio, requisitos de escala."

### **"Como garante consistência de dados?"**
**R:** "Uso Eventual Consistency via eventos. Quando uma venda é confirmada, publico evento para atualizar estoque. Se houver falha, o RabbitMQ garante retry. Para casos que precisam de consistência forte, implementaria Saga Pattern ou 2-Phase Commit."

### **"Como trata falhas na comunicação?"**
**R:** "RabbitMQ garante durabilidade das mensagens. Para comunicação HTTP síncrona, implemento timeout e retry. Em produção, adicionaria Circuit Breaker pattern e health checks mais robustos."

### **"Como monitora o sistema?"**
**R:** "Health checks em cada serviço, logs estruturados, métricas via RabbitMQ Management. Em produção, adicionaria APM tools como Application Insights, ELK Stack para logs, e Prometheus/Grafana para métricas."

### **"Como faz deploy?"**
**R:** "Docker facilita deploy. Em produção, usaria Kubernetes para orquestração, CI/CD pipelines com GitLab/Azure DevOps, e estratégias como Blue/Green ou Rolling Updates."

---

## 🏆 **CONCLUSÃO DA APRESENTAÇÃO**

**"Este projeto demonstra:**
- Arquitetura de microserviços bem estruturada
- Comunicação assíncrona robusta
- Segurança com JWT
- Containerização completa
- Código limpo e documentado
- Testes automatizados

**É um sistema pronto para produção que pode escalar e evoluir conforme necessidades do negócio."**

---

## ⏱️ **CRONOGRAMA SUGERIDO (30 minutos total)**

1. **Introdução** (2 min)
2. **Arquitetura Geral** (5 min)
3. **API Gateway** (5 min)
4. **Microserviço Vendas** (7 min)
5. **Microserviço Estoque** (7 min)
6. **RabbitMQ** (5 min)
7. **Banco e Segurança** (4 min)
8. **Docker** (3 min)
9. **Demo Prática** (10 min)
10. **Perguntas** (10 min)

**🎯 Boa sorte na sua entrevista! Este projeto demonstra conhecimento técnico sólido e experiência prática com tecnologias modernas!**