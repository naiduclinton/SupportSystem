using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SupportTicketing.Core.Interfaces;

namespace SupportTicketing.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}

public class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private bool _disposed;

    public RabbitMqMessageBus(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqMessageBus> logger)
    {
        _logger = logger;

        var cfg = settings.Value;
        var factory = new ConnectionFactory
        {
            HostName = cfg.Host,
            Port = cfg.Port,
            UserName = cfg.Username,
            Password = cfg.Password,
            VirtualHost = cfg.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection("support-ticketing-api");
        _channel = _connection.CreateModel();

        // Declare exchanges
        _channel.ExchangeDeclare("tickets", ExchangeType.Topic, durable: true);

        // Declare queues and bind
        DeclareAndBind("ticket.notifications", "tickets", "ticket.*");
        DeclareAndBind("ticket.sla",           "tickets", "ticket.sla_breached");
        DeclareAndBind("ticket.csat",          "tickets", "ticket.status_changed");
    }

    public Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var body = Encoding.UTF8.GetBytes(json);

            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            props.MessageId = Guid.NewGuid().ToString();
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(exchange, routingKey, props, body);

            _logger.LogDebug("Published {RoutingKey} to {Exchange}", routingKey, exchange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {RoutingKey}", routingKey);
        }

        return Task.CompletedTask;
    }

    private void DeclareAndBind(string queueName, string exchange, string bindingKey)
    {
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, exchange, bindingKey);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _channel.Close();
        _connection.Close();
        _channel.Dispose();
        _connection.Dispose();
        _disposed = true;
    }
}
