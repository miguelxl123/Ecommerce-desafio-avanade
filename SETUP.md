# Configuração para execução local (appsettings.Development.json)

## API Gateway (porta 5000)
- Endpoint: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health

## Microserviço de Vendas (porta 5001)
- Endpoint: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- Health Check: http://localhost:5001/health

## Microserviço de Estoque (porta 5002)
- Endpoint: http://localhost:5002
- Swagger: http://localhost:5002/swagger
- Health Check: http://localhost:5002/health

## Banco de Dados PostgreSQL
- Host: localhost
- Porta: 5432
- Usuário: postgres
- Senha: postgres
- Databases:
  - vendasdb (para microserviço de vendas)
  - inventario (para microserviço de estoque)

## RabbitMQ
- AMQP: amqp://localhost:5672
- Management UI: http://localhost:15672
- Usuário: admin
- Senha: admin123

## Configuração de Desenvolvimento

### Pré-requisitos
1. .NET 9.0 SDK
2. PostgreSQL 15+
3. RabbitMQ 3.x

### Como executar localmente

1. **Instalar dependências:**
   ```bash
   dotnet restore
   ```

2. **Executar PostgreSQL e RabbitMQ:**
   ```bash
   docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:15-alpine
   docker run -d --name rabbitmq -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=admin123 -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
   ```

3. **Criar bancos de dados:**
   ```sql
   CREATE DATABASE vendasdb;
   CREATE DATABASE inventario;
   ```

4. **Executar migrations:**
   ```bash
   cd src/Ecommerce.Vendas
   dotnet ef database update
   
   cd ../Ecommerce.Estoque
   dotnet ef database update
   ```

5. **Executar serviços (em terminais separados):**
   ```bash
   # API Gateway
   cd src/ApiGateway
   dotnet run
   
   # Microserviço de Vendas
   cd src/Ecommerce.Vendas
   dotnet run
   
   # Microserviço de Estoque
   cd src/Ecommerce.Estoque
   dotnet run
   ```

### Como executar com Docker

1. **Build e executar todos os serviços:**
   ```bash
   docker-compose up -d
   ```

2. **Parar todos os serviços:**
   ```bash
   docker-compose down
   ```

3. **Ver logs:**
   ```bash
   docker-compose logs -f
   ```

## 🧪 Testando a aplicação

### **Opção 1: Testando via Swagger UI (Recomendado)**

1. **Acesse as interfaces Swagger:**
   - API Gateway: http://localhost:5000/swagger
   - Vendas: http://localhost:5001/swagger
   - Estoque: http://localhost:5002/swagger

2. **Passos para testar no Swagger:**
   - Faça login no API Gateway
   - Copie o token JWT retornado
   - Clique em "Authorize" no Swagger
   - Cole o token no formato: `Bearer SEU_TOKEN_AQUI`
   - Teste os endpoints protegidos

### **Opção 2: Testando via cURL**

#### 1. **Autenticação - Fazer Login**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@ecommerce.com",
    "password": "admin123"
  }'
```

**Resposta esperada:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "email": "admin@ecommerce.com",
    "name": "Administrador",
    "role": "Admin",
    "expiresAt": "2025-10-30T..."
  },
  "message": "Login realizado com sucesso"
}
```

#### 2. **Listar Produtos (Público)**
```bash
curl http://localhost:5000/api/inventario/products
```

#### 3. **Obter Produto Específico**
```bash
curl http://localhost:5000/api/inventario/products/11111111-1111-1111-1111-111111111111
```

#### 4. **Criar Pedido (Requer Autenticação)**
```bash
# Substitua YOUR_TOKEN pelo token obtido no login
curl -X POST http://localhost:5000/api/vendas/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "items": [
      {
        "productId": "11111111-1111-1111-1111-111111111111",
        "quantity": 2
      },
      {
        "productId": "22222222-2222-2222-2222-222222222222",
        "quantity": 1
      }
    ],
    "notes": "Pedido de teste via API"
  }'
```

#### 5. **Listar Pedidos do Usuário**
```bash
curl -H "Authorization: Bearer YOUR_TOKEN" \
  http://localhost:5000/api/vendas/orders
```

#### 6. **Confirmar Pedido**
```bash
# Substitua ORDER_ID pelo ID do pedido criado
curl -X POST http://localhost:5000/api/vendas/orders/ORDER_ID/confirm \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### 7. **Verificar Disponibilidade de Produto**
```bash
curl -X POST http://localhost:5000/api/inventario/products/availability \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "11111111-1111-1111-1111-111111111111",
    "quantity": 5
  }'
```

#### 8. **Criar Novo Produto (Admin)**
```bash
curl -X POST http://localhost:5000/api/inventario/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Produto de Teste",
    "description": "Produto criado via API",
    "price": 99.99,
    "stockQuantity": 100,
    "category": "Teste",
    "sku": "TEST-001"
  }'
```

### **Opção 3: Testando via Postman**

1. **Importe a collection:** Crie uma nova collection no Postman
2. **Configure as variáveis:**
   - `base_url`: http://localhost:5000
   - `token`: (será preenchido após login)

3. **Requests sugeridos:**
   - POST {{base_url}}/api/auth/login
   - GET {{base_url}}/api/inventario/products
   - POST {{base_url}}/api/vendas/orders
   - GET {{base_url}}/api/vendas/orders

### **Cenários de Teste Completos**

#### **Cenário 1: Fluxo Completo de Venda**
1. Fazer login como admin
2. Listar produtos disponíveis
3. Verificar disponibilidade de um produto
4. Criar pedido com múltiplos itens
5. Confirmar pedido
6. Verificar que o estoque foi reduzido automaticamente
7. Verificar logs do RabbitMQ

#### **Cenário 2: Validações de Negócio**
1. Tentar criar pedido sem autenticação (deve falhar)
2. Tentar criar pedido com quantidade maior que estoque
3. Tentar confirmar pedido inexistente
4. Tentar acessar pedido de outro usuário

#### **Cenário 3: Administração de Produtos**
1. Fazer login como admin
2. Criar novo produto
3. Atualizar produto existente
4. Adicionar estoque manualmente
5. Desativar produto

### **Monitoramento durante os Testes**

#### **Verificar RabbitMQ:**
- Acesse: http://localhost:15672
- Login: admin / admin123
- Verifique filas e mensagens processadas

#### **Verificar Logs:**
```bash
# Via Docker Compose
docker-compose logs -f api-gateway
docker-compose logs -f microservico-vendas
docker-compose logs -f microservico-estoque

# Via .NET (se rodando localmente)
# Os logs aparecerão no console de cada aplicação
```

#### **Verificar Health Checks:**
```bash
curl http://localhost:5000/health  # API Gateway
curl http://localhost:5001/health  # Vendas
curl http://localhost:5002/health  # Estoque
```

#### **Verificar Banco de Dados:**
```sql
-- Conectar ao PostgreSQL
psql -h localhost -U postgres

-- Verificar vendas
\c vendasdb
SELECT * FROM "Orders";
SELECT * FROM "OrderItems";

-- Verificar estoque
\c inventario
SELECT * FROM "Products";
```

### **Dados de Teste Pré-configurados**

#### **Usuários:**
- **Admin**: admin@ecommerce.com / admin123
- **User**: user@ecommerce.com / user123

#### **Produtos (IDs fixos para teste):**
- Samsung Galaxy S23: `11111111-1111-1111-1111-111111111111`
- Lenovo ThinkPad: `22222222-2222-2222-2222-222222222222`
- Sony Fone: `33333333-3333-3333-3333-333333333333`
- Smart TV Samsung: `44444444-4444-4444-4444-444444444444`
- PlayStation 5: `55555555-5555-5555-5555-555555555555`