# Ecommerce Microservices - Desafio Avanade

Projeto para desenvolver uma aplicação com arquitetura de microserviços para gerenciamento de estoque de produtos e vendas em uma plataforma de e-commerce.

O sistema é composto por microserviços que se comunicam via um API Gateway e um broker de mensagens (RabbitMQ). A autenticação é baseada em JWT.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/miguelxl123/ecommerce-desafio-avanade)


# Arquitetura

- **API Gateway** (Porta 5000) - Roteamento e autenticação JWT
- **Microserviço Vendas** (Porta 5001) - Gestão de pedidos
- **Microserviço Estoque** (Porta 5002) - Gestão de produtos
- **PostgreSQL** - Banco de dados com schemas separados
- **RabbitMQ** - Mensageria assíncrona
- **Docker** - Orquestração dos serviços

##  Arquitetura do Sistema

![Arquitetura](https://hermes.dio.me/files/assets/45346875-7aad-45d4-8845-feadf18488e5.png)

*Diagrama da arquitetura de microserviços do sistema de ecommerce*

##  Tecnologias

- .NET Core 9.0
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- JWT Authentication
- YARP (API Gateway)
- Docker & Docker Compose
- Swagger/OpenAPI

## Badges

![JWT](https://img.shields.io/badge/JWT-black?style=for-the-badge&logo=JSON%20web%20tokens)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)	![Visual Studio Code](https://img.shields.io/badge/Visual%20Studio%20Code-0078d7.svg?style=for-the-badge&logo=visual-studio-code&logoColor=white) ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)	![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)

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

- **Testes:** [![Tests](https://img.shields.io/badge/tests-integration-blue)](./MANUAL-TESTES.md)
- **Setup:** `SETUP.md`[![Setup](https://img.shields.io/badge/tests-integration-blue)](./MANUAL-TESTES.md)
- **Postman:** `postman-collection.json`[![Json](https://img.shields.io/badge/dynamic/json)](postman-collection.json)


##  Fluxo de Teste

1. Login → Obter token JWT
2. Listar produtos → Verificar disponibilidade
3. Criar pedido → Confirmar pedido
4. Verificar baixa automática no estoque via RabbitMQ

##  Usuários

- **Admin:** admin@ecommerce.com / admin123
- **User:** user@ecommerce.com / user123

## Sobre mim

- Nome: José Miguel
- Perfil: ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)	![Visual Studio Code](https://img.shields.io/badge/Visual%20Studio%20Code-0078d7.svg?style=for-the-badge&logo=visual-studio-code&logoColor=white) ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)	![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)
- Contato: 	[![LinkedIn](https://img.shields.io/badge/linkedin-%230077B5.svg?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/jos%C3%A9-miguel-059a19170/)
- bootcamp: [<img src="https://assets.dio.me/fmurnmImYsLpbR26s6Rsrxi82t-6iYqTlwkJGBzm0mI/f:webp/h:120/q:80/L3RyYWNrcy8yMzk0ODU4NS1iZTdmLTRlZjctODQxNi1iOGUwYWFhYWYyZjcucG5n" height="50"></a>](https://web.dio.me/track/avanade-back-end-com-net-e-ia)
