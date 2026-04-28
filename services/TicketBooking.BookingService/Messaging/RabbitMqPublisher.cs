using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TicketBooking.Shared.Constants;
using TicketBooking.Shared.Messages;

namespace TicketBooking.BookingService.Messaging;

public interface IMessagePublisher
{
    Task PublishBookingCreatedAsync(BookingCreatedMessage message);
}

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private bool _initialized;

    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMq:Host"] ?? "rabbitmq",
                Port = int.Parse(_config["RabbitMq:Port"] ?? "5672"),
                UserName = _config["RabbitMq:Username"] ?? "guest",
                Password = _config["RabbitMq:Password"] ?? "guest"
            };
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(RabbitMqConstants.BookingExchange, ExchangeType.Direct, durable: true);
            await _channel.QueueDeclareAsync(RabbitMqConstants.BookingCreatedQueue, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(RabbitMqConstants.BookingCreatedQueue, RabbitMqConstants.BookingExchange, RabbitMqConstants.BookingCreatedRoutingKey);
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ not available: {Msg}", ex.Message);
        }
    }

    public async Task PublishBookingCreatedAsync(BookingCreatedMessage message)
    {
        await EnsureInitializedAsync();
        if (_channel is null)
        {
            _logger.LogWarning("RabbitMQ channel not available, skipping publish");
            return;
        }
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties { Persistent = true };
        await _channel.BasicPublishAsync(
            exchange: RabbitMqConstants.BookingExchange,
            routingKey: RabbitMqConstants.BookingCreatedRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body);
        _logger.LogInformation("Published BookingCreated: {BookingId}", message.BookingId);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
