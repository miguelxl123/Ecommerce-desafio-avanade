using Ecommerce.Shared.Events;
using Ecommerce.Shared.Services;
using Ecommerce.Estoque.Services;

namespace Ecommerce.Estoque.Services;

public interface IRabbitMQConsumerService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

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
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Configurar consumo de mensagens de venda criada
            await _rabbitMQ.SubscribeAsync<VendaCriadaEvent>("venda.criada", ProcessarVendaCriada);
            
            _logger.LogInformation("RabbitMQ Consumer Service started and listening for messages");

            // Manter o serviço rodando
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RabbitMQ Consumer Service");
            throw;
        }
    }

    private async Task ProcessarVendaCriada(VendaCriadaEvent vendaEvent)
    {
        try
        {
            _logger.LogInformation("Processando venda criada: {OrderId}", vendaEvent.OrderId);

            using var scope = _serviceProvider.CreateScope();
            var estoqueService = scope.ServiceProvider.GetRequiredService<IEstoqueService>();

            // Processar cada item da venda
            foreach (var item in vendaEvent.Items)
            {
                var result = await estoqueService.DarBaixaEstoqueAsync(
                    item.ProductId, 
                    item.Quantity, 
                    $"Venda #{vendaEvent.OrderId}");

                if (!result.Success)
                {
                    _logger.LogError("Falha ao dar baixa no estoque: Produto {ProductId}, Pedido {OrderId}, Erro: {Error}", 
                        item.ProductId, vendaEvent.OrderId, result.Message);
                    
                    // Em um cenário real, você poderia implementar:
                    // - Compensação (saga pattern)
                    // - Dead letter queue
                    // - Retry com backoff exponencial
                    // - Notificação para sistemas de monitoramento
                }
                else
                {
                    _logger.LogInformation("Baixa de estoque processada com sucesso: Produto {ProductId}, Quantidade {Quantity}, Pedido {OrderId}", 
                        item.ProductId, item.Quantity, vendaEvent.OrderId);
                }
            }

            _logger.LogInformation("Processamento da venda {OrderId} concluído", vendaEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar venda criada: {OrderId}", vendaEvent.OrderId);
            throw; // Re-throw para que o RabbitMQ possa fazer retry ou mover para DLQ
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting RabbitMQ Consumer Service");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ Consumer Service");
        await base.StopAsync(cancellationToken);
    }
}