using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using TicketBooking.PaymentService.Data;
using TicketBooking.PaymentService.DTOs;
using TicketBooking.PaymentService.Models;
using TicketBooking.Shared.Constants;
using TicketBooking.Shared.Messages;

namespace TicketBooking.PaymentService.Services;

public interface IPaymentService
{
    Task<(bool Success, PaymentResponse? Payment, string? Error)> ProcessPaymentAsync(ProcessPaymentRequest request, Guid userId);
    Task<PaymentResponse?> GetPaymentByBookingAsync(Guid bookingId);
}

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _db;
    private readonly HttpClient _bookingClient;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(PaymentDbContext db, IHttpClientFactory factory, IConfiguration config, ILogger<PaymentService> logger)
    {
        _db = db;
        _bookingClient = factory.CreateClient("BookingService");
        _config = config;
        _logger = logger;
    }

    public async Task<(bool Success, PaymentResponse? Payment, string? Error)> ProcessPaymentAsync(ProcessPaymentRequest request, Guid userId)
    {
        // Simulate payment processing delay
        await Task.Delay(500);

        var txRef = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var payment = new Payment
        {
            BookingId = request.BookingId,
            UserId = userId,
            Amount = request.Amount,
            Status = "Completed",
            TransactionReference = txRef,
            ProcessedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // Notify booking service to confirm
        try
        {
            var confirmPayload = new { PaymentReference = txRef };
            var json = JsonSerializer.Serialize(confirmPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _bookingClient.PostAsync($"/api/bookings/{request.BookingId}/confirm", content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not confirm booking: {Msg}", ex.Message);
        }

        // Publish PaymentCompleted to RabbitMQ
        await PublishPaymentCompletedAsync(new PaymentCompletedMessage(
            payment.Id, request.BookingId, userId, request.Amount, "Completed", DateTime.UtcNow));

        var response = new PaymentResponse(payment.Id, payment.BookingId, payment.Amount, payment.Status, payment.TransactionReference, payment.CreatedAt);
        return (true, response, null);
    }

    public async Task<PaymentResponse?> GetPaymentByBookingAsync(Guid bookingId)
    {
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.BookingId == bookingId);
        return p is null ? null : new PaymentResponse(p.Id, p.BookingId, p.Amount, p.Status, p.TransactionReference, p.CreatedAt);
    }

    private async Task PublishPaymentCompletedAsync(PaymentCompletedMessage message)
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
            await using var conn = await factory.CreateConnectionAsync();
            var channel = await conn.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(RabbitMqConstants.PaymentExchange, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(RabbitMqConstants.PaymentCompletedQueue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(RabbitMqConstants.PaymentCompletedQueue, RabbitMqConstants.PaymentExchange, RabbitMqConstants.PaymentCompletedRoutingKey);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(RabbitMqConstants.PaymentExchange, RabbitMqConstants.PaymentCompletedRoutingKey, body);
            _logger.LogInformation("Published PaymentCompleted: {PaymentId}", message.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ publish failed: {Msg}", ex.Message);
        }
    }
}
