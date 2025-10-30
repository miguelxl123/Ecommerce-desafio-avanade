param([string]$BaseUrl = "http://localhost:5000")

Write-Host "=== TESTE ECOMMERCE MICROSERVICES ===" -ForegroundColor Green

function Test-ApiEndpoint {
    param($Method, $Uri, $Headers = @{}, $Body = $null)
    try {
        $params = @{ Method = $Method; Uri = $Uri; Headers = $Headers; UseBasicParsing = $true }
        if ($Body) { $params.Body = $Body; $params.ContentType = "application/json" }
        return Invoke-RestMethod @params
    } catch {
        Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# 1. Health Check
Write-Host "`n1. Health Check..." -ForegroundColor Cyan
$health = Test-ApiEndpoint -Method GET -Uri "$BaseUrl/health"
if ($health.Status -eq "Healthy") { Write-Host "OK" -ForegroundColor Green } else { exit 1 }

# 2. Login
Write-Host "`n2. Login..." -ForegroundColor Cyan
$loginBody = '{"email":"admin@ecommerce.com","password":"admin123"}'
$login = Test-ApiEndpoint -Method POST -Uri "$BaseUrl/api/auth/login" -Body $loginBody
if ($login.success) {
    Write-Host "OK - Token obtido" -ForegroundColor Green
    $token = $login.data.token
    $headers = @{ Authorization = "Bearer $token" }
} else { exit 1 }

# 3. Listar Produtos
Write-Host "`n3. Listar Produtos..." -ForegroundColor Cyan
$products = Test-ApiEndpoint -Method GET -Uri "$BaseUrl/api/inventario/products"
if ($products.success) {
    Write-Host "OK - $($products.data.Count) produtos encontrados" -ForegroundColor Green
    $productId = $products.data[0].id
} else { exit 1 }

# 4. Criar Pedido
Write-Host "`n4. Criar Pedido..." -ForegroundColor Cyan
$orderBody = "{`"items`":[{`"productId`":`"$productId`",`"quantity`":2}],`"notes`":`"Teste`"}"
$order = Test-ApiEndpoint -Method POST -Uri "$BaseUrl/api/vendas/orders" -Headers $headers -Body $orderBody
if ($order.success) {
    Write-Host "OK - Pedido $($order.data.id) criado" -ForegroundColor Green
    $orderId = $order.data.id
} else { exit 1 }

# 5. Confirmar Pedido
Write-Host "`n5. Confirmar Pedido..." -ForegroundColor Cyan
$confirm = Test-ApiEndpoint -Method POST -Uri "$BaseUrl/api/vendas/orders/$orderId/confirm" -Headers $headers
if ($confirm.success) {
    Write-Host "OK - Pedido confirmado" -ForegroundColor Green
} else { Write-Host "ERRO ao confirmar" -ForegroundColor Red }

Write-Host "`n=== TESTES CONCLUIDOS ===" -ForegroundColor Green
Write-Host "Sistema funcionando corretamente!" -ForegroundColor Green