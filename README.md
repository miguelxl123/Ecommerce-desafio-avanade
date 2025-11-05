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

## 🏗️ Arquitetura do Sistema

![Arquitetura](arquitetura.png)

*Diagrama da arquitetura de microserviços do sistema de ecommerce*

##  Tecnologias

-  ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
-  ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
- Entity Framework Core
-  ![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)
-  ![RabbitMQ](https://img.shields.io/badge/Rabbitmq-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
-  ![JWT](https://img.shields.io/badge/JWT-black?style=for-the-badge&logo=JSON%20web%20tokens)
- YARP (API Gateway) 
-  ![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)
-  ![Swagger](https://img.shields.io/badge/-Swagger-%23Clojure?style=for-the-badge&logo=swagger&logoColor=white)
-  ![Visual Studio Code](https://img.shields.io/badge/Visual%20Studio%20Code-0078d7.svg?style=for-the-badge&logo=visual-studio-code&logoColor=white)

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

- **Testes:** [![Tests](https://img.shields.io/badge/tests-red?&style=Plastic)](./MANUAL-TESTES.md)
- **Setup:** [![Tests](https://img.shields.io/badge/SETUP-black?&style=Plastic)](./SETUP.md)
- **Postman:** [![Tests](https://img.shields.io/badge/postman-orange?&style=Plastic)](./postman-collection.json)



##  Fluxo de Teste

1. Login → Obter token JWT
2. Listar produtos → Verificar disponibilidade
3. Criar pedido → Confirmar pedido
4. Verificar baixa automática no estoque via RabbitMQ

##  Usuários

- **Admin:** admin@ecommerce.com / admin123
- **User:** user@ecommerce.com / user123

## Auxílio 

- [![YouTube](https://img.shields.io/badge/YouTube-%23FF0000.svg?style=for-the-badge&logo=YouTube&logoColor=white)](https://www.youtube.com/watch?v=jap8tXIAMi4&list=PLJ4k1IC8GhW1UtPi9nwwW9l4TwRLR9Nxg)
- [![YouTube](https://img.shields.io/badge/YouTube-%23FF0000.svg?style=for-the-badge&logo=YouTube&logoColor=white)](https://www.youtube.com/watch?v=-NaKiyaIZpM&list=PLBIZ3dmiYIYnMaxogi0YTT7n9aZAoM7TY)

## Sobre mim

- Nome: José Miguel
- Perfil: ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)	![Visual Studio Code](https://img.shields.io/badge/Visual%20Studio%20Code-0078d7.svg?style=for-the-badge&logo=visual-studio-code&logoColor=white) ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)	![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)
- Contato: 	[![LinkedIn](https://img.shields.io/badge/linkedin-%230077B5.svg?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/jos%C3%A9-miguel-059a19170/)
- bootcamp: [<img src="https://assets.dio.me/fmurnmImYsLpbR26s6Rsrxi82t-6iYqTlwkJGBzm0mI/f:webp/h:120/q:80/L3RyYWNrcy8yMzk0ODU4NS1iZTdmLTRlZjctODQxNi1iOGUwYWFhYWYyZjcucG5n" height="50"></a>](https://web.dio.me/track/avanade-back-end-com-net-e-ia)
