# 📖 Manual de Testes - Ecommerce Microservices

## 🚀 Execução

### **1. Iniciar Sistema:**
```bash
docker-compose up -d
```

### **2. Teste Automatizado:**
```powershell
.\test-quick.ps1
```

---

## 🌐 Testes Manuais via Swagger

### **Acesso:**
- **API Gateway:** http://localhost:5000/swagger
- **Vendas:** http://localhost:5001/swagger
- **Estoque:** http://localhost:5002/swagger

### **1. Login (API Gateway):**
- Endpoint: `POST /api/auth/login`
- JSON:
```json
{
  "email": "admin@ecommerce.com",
  "password": "admin123"
}
```

### **2. Configurar Autorização:**
- Clique "Authorize"
- Cole: `Bearer SEU_TOKEN_AQUI`

### **3. Testar Endpoints:**

#### **Produtos:**
- `GET /api/inventario/products` - Listar produtos
- `POST /api/inventario/products/availability` - Verificar estoque:
```json
{
  "productId": "11111111-1111-1111-1111-111111111111",
  "quantity": 2
}
```

#### **Pedidos:**
- `POST /api/vendas/orders` - Criar pedido:
```json
{
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "quantity": 1
    }
  ],
  "notes": "Teste manual"
}
```
- `GET /api/vendas/orders` - Listar pedidos
- `POST /api/vendas/orders/{id}/confirm` - Confirmar pedido

---

## 👥 Usuários de Teste

| Email | Senha | Role |
|-------|-------|------|
| admin@ecommerce.com | admin123 | Admin |
| user@ecommerce.com | user123 | User |

---

## 📦 Produtos Disponíveis

| Nome | ID | Estoque |
|------|----| --------|
| Samsung Galaxy S23 | 11111111-1111-1111-1111-111111111111 | 50 |
| Lenovo ThinkPad | 22222222-2222-2222-2222-222222222222 | 25 |
| Sony Fone | 33333333-3333-3333-3333-333333333333 | 100 |
| Smart TV Samsung | 44444444-4444-4444-4444-444444444444 | 15 |
| PlayStation 5 | 55555555-5555-5555-5555-555555555555 | 8 |

---

## 📊 Monitoramento

- **RabbitMQ:** http://localhost:15672 (admin/admin123)
- **Logs:** `docker-compose logs -f`
- **Health:** http://localhost:5000/health

---

## 🔧 Solução de Problemas

### **Erro 401 (Unauthorized):**
1. Verifique se fez login corretamente
2. Token deve estar no formato: `Bearer TOKEN`
3. Token deve estar completo (200+ caracteres)

### **Serviços não respondem:**
```bash
docker-compose restart
```

### **Verificar sistema:**
```powershell
.\test-quick.ps1
```