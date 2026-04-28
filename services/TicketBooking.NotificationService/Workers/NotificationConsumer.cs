using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketBooking.Shared.Constants;
using TicketBooking.Shared.Messages;

namespace TicketBooking.NotificationService.Workers;

public class NotificationConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public NotificationConsumer(IConfiguration config, ILogger<NotificationConsumer> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel is null) return;

        // Consume BookingCreated
        await _channel.ExchangeDeclareAsync(RabbitMqConstants.BookingExchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqConstants.BookingCreatedQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqConstants.BookingCreatedQueue, RabbitMqConstants.BookingExchange, RabbitMqConstants.BookingCreatedRoutingKey, cancellationToken: stoppingToken);

        // Consume PaymentCompleted
        await _channel.ExchangeDeclareAsync(RabbitMqConstants.PaymentExchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqConstants.PaymentCompletedQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqConstants.PaymentCompletedQueue, RabbitMqConstants.PaymentExchange, RabbitMqConstants.PaymentCompletedRoutingKey, cancellationToken: stoppingToken);

        var bookingConsumer = new AsyncEventingBasicConsumer(_channel);
        bookingConsumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<BookingCreatedMessage>(body);
            if (message is not null)
            {
                _logger.LogInformation(
                    "[EMAIL] Booking confirmation sent to {Email} | Booking {BookingId} | Event: {Event} | Seat: {Seat} | Amount: ${Amount}",
                    message.UserEmail, message.BookingId, message.EventName, message.SeatNumber, message.Amount);
            }
            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        var paymentConsumer = new AsyncEventingBasicConsumer(_channel);
        paymentConsumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<PaymentCompletedMessage>(body);
            if (message is not null)
            {
                _logger.LogInformation(
                    "[EMAIL] Payment receipt sent | Payment {PaymentId} | Booking {BookingId} | Amount: ${Amount} | Status: {Status}",
                    message.PaymentId, message.BookingId, message.Amount, message.Status);
            }
            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(RabbitMqConstants.BookingCreatedQueue, autoAck: false, bookingConsumer, stoppingToken);
        await _channel.BasicConsumeAsync(RabbitMqConstants.PaymentCompletedQueue, autoAck: false, paymentConsumer, stoppingToken);

        _logger.LogInformation("NotificationService: Listening for messages...");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectWithRetryAsync(CancellationToken token)
    {
        int retries = 0;
        while (retries < 10 && !token.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config["RabbitMq:Host"] ?? "rabbitmq",
                    Port = int.Parse(_config["RabbitMq:Port"] ?? "5672"),
                    UserName = _config["RabbitMq:Username"] ?? "guest",
                    Password = _config["RabbitMq:Password"] ?? "guest"
                };
                _connection = await factory.CreateConnectionAsync(token);
                _channel = await _connection.CreateChannelAsync(cancellationToken: token);
                _logger.LogInformation("Connected to RabbitMQ");
                return;
            }
            catch
            {
                retries++;
                _logger.LogWarning("RabbitMQ not ready, retrying ({Attempt}/10)...", retries);
                await Task.Delay(5000, token);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
