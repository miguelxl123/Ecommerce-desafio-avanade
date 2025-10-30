# Ecommerce Microservices - Desafio Avanade

Sistema de ecommerce com arquitetura de microserviços usando .NET Core, Entity Framework, RabbitMQ e Docker.

# Arquitetura

- **API Gateway** (Porta 5000) - Roteamento e autenticação JWT
- **Microserviço Vendas** (Porta 5001) - Gestão de pedidos
- **Microserviço Estoque** (Porta 5002) - Gestão de produtos
- **PostgreSQL** - Banco de dados com schemas separados
- **RabbitMQ** - Mensageria assíncrona
- **Docker** - Orquestração dos serviços

##  Execução

### **Iniciar Sistema:**
```bash
docker-compose up -d
```

### **Teste Automatizado:**
```powershell
.\test-quick.ps1
```

### **Acessos:**
- **API Gateway:** http://localhost:5000/swagger
- **Vendas:** http://localhost:5001/swagger  
- **Estoque:** http://localhost:5002/swagger
- **RabbitMQ:** http://localhost:15672 (admin/admin123)

##  Documentação

- **Testes:** `MANUAL-TESTES.md`
- **Setup:** `SETUP.md`
- **Postman:** `postman-collection.json`

##  Fluxo de Teste

1. Login → Obter token JWT
2. Listar produtos → Verificar disponibilidade
3. Criar pedido → Confirmar pedido
4. Verificar baixa automática no estoque via RabbitMQ

##  Usuários

- **Admin:** admin@ecommerce.com / admin123
- **User:** user@ecommerce.com / user123

##  Tecnologias

- .NET Core 9.0
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- JWT Authentication
- YARP (API Gateway)
- Docker & Docker Compose
- Swagger/OpenAPI