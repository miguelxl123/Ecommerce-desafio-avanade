using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Shared.Services;

/// <summary>
/// Interface para serviços de mensageria com RabbitMQ
/// </summary>
public interface IRabbitMQService
{
    /// <summary>
    /// Publicar mensagem em uma exchange/rota específica
    /// </summary>
    Task PublishAsync<T>(string exchange, string routingKey, T message);
    
    /// <summary>
    /// Subscrever a uma fila para receber mensagens
    /// </summary>
    Task SubscribeAsync<T>(string queue, Func<T, Task> handler);
    
    /// <summary>
    /// Liberar recursos da conexão
    /// </summary>
    void Dispose();
}

/// <summary>
/// Implementação do serviço de mensageria RabbitMQ para comunicação entre microserviços
/// </summary>
public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly string _connectionString;

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("RabbitMQ") ?? "amqp://localhost:5672";
        
        try
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_connectionString),
                DispatchConsumersAsync = true
            };
            
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("Connected to RabbitMQ at {ConnectionString}", _connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {ConnectionString}", _connectionString);
            throw;
        }
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        try
        {
            // Declare exchange if it doesn't exist
            _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(exchange, routingKey, properties, body);
            
            _logger.LogInformation("Published message to exchange {Exchange} with routing key {RoutingKey}", 
                exchange, routingKey);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to exchange {Exchange} with routing key {RoutingKey}", 
                exchange, routingKey);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(string queue, Func<T, Task> handler)
    {
        try
        {
            // Declare queue if it doesn't exist
            _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.Received += async (model, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await handler(message);
                        _channel.BasicAck(eventArgs.DeliveryTag, false);
                        
                        _logger.LogInformation("Successfully processed message from queue {Queue}", queue);
                    }
                    else
                    {
                        _logger.LogWarning("Received null message from queue {Queue}", queue);
                        _channel.BasicNack(eventArgs.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {Queue}", queue);
                    _channel.BasicNack(eventArgs.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queue, false, consumer);
            
            _logger.LogInformation("Started consuming messages from queue {Queue}", queue);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to queue {Queue}", queue);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ connection disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}