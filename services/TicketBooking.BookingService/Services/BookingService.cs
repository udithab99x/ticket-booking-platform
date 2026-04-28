using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TicketBooking.BookingService.Data;
using TicketBooking.BookingService.DTOs;
using TicketBooking.BookingService.Messaging;
using TicketBooking.BookingService.Models;
using TicketBooking.Shared.DTOs;
using TicketBooking.Shared.Messages;

namespace TicketBooking.BookingService.Services;

public interface IBookingService
{
    Task<(bool Success, BookingResponse? Booking, string? Error)> CreateBookingAsync(CreateBookingRequest request, Guid userId, string userEmail);
    Task<BookingResponse?> GetBookingAsync(Guid id, Guid userId);
    Task<PagedResponse<BookingResponse>> GetUserBookingsAsync(Guid userId, int page, int pageSize);
    Task<bool> ConfirmBookingAsync(Guid bookingId, string paymentReference);
    Task<bool> CancelBookingAsync(Guid bookingId, Guid userId);
}

public class BookingService : IBookingService
{
    private readonly BookingDbContext _db;
    private readonly IDatabase _redis;
    private readonly IMessagePublisher _publisher;
    private readonly HttpClient _eventHttpClient;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        BookingDbContext db,
        IConnectionMultiplexer redis,
        IMessagePublisher publisher,
        IHttpClientFactory httpFactory,
        ILogger<BookingService> logger)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _publisher = publisher;
        _eventHttpClient = httpFactory.CreateClient("EventService");
        _logger = logger;
    }

    public async Task<(bool Success, BookingResponse? Booking, string? Error)> CreateBookingAsync(
        CreateBookingRequest request, Guid userId, string userEmail)
    {
        // Distributed lock key: one booking per seat at a time
        var lockKey = $"seat-lock:{request.EventId}:{request.SeatId}";
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await _redis.StringSetAsync(lockKey, lockValue, TimeSpan.FromSeconds(10), When.NotExists);

        if (!acquired)
            return (false, null, "Seat is currently being reserved by another user. Please try again.");

        try
        {
            // Check seat availability in Redis cache
            var seatCacheKey = $"seat-booked:{request.EventId}:{request.SeatId}";
            var alreadyBooked = await _redis.StringGetAsync(seatCacheKey);
            if (alreadyBooked == "1")
                return (false, null, "Seat is already booked.");

            // Get event details
            EventDetails? eventDetails = null;
            try
            {
                var response = await _eventHttpClient.GetAsync($"/api/events/{request.EventId}");
                if (response.IsSuccessStatusCode)
                {
                    var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<EventDetails>>();
                    eventDetails = apiResp?.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not reach EventService: {Msg}", ex.Message);
            }

            var booking = new Booking
            {
                UserId = userId,
                UserEmail = userEmail,
                EventId = request.EventId,
                EventName = eventDetails?.Name ?? "Event",
                EventDate = eventDetails?.EventDate.ToString("yyyy-MM-dd") ?? "TBD",
                SeatNumber = request.SeatNumber,
                SeatId = request.SeatId,
                Amount = eventDetails?.TicketPrice ?? 0,
                Status = BookingStatus.Pending
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            // Cache the seat as booked
            await _redis.StringSetAsync(seatCacheKey, "1", TimeSpan.FromDays(30));

            // Publish event to RabbitMQ (async notification)
            await _publisher.PublishBookingCreatedAsync(new BookingCreatedMessage(
                booking.Id, userId, userEmail, request.EventId,
                booking.EventName, booking.EventDate, request.SeatNumber,
                booking.Amount, booking.CreatedAt));

            return (true, MapToResponse(booking), null);
        }
        finally
        {
            // Release the distributed lock
            var currentValue = await _redis.StringGetAsync(lockKey);
            if (currentValue == lockValue)
                await _redis.KeyDeleteAsync(lockKey);
        }
    }

    public async Task<BookingResponse?> GetBookingAsync(Guid id, Guid userId)
    {
        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return b is null ? null : MapToResponse(b);
    }

    public async Task<PagedResponse<BookingResponse>> GetUserBookingsAsync(Guid userId, int page, int pageSize)
    {
        var q = _db.Bookings.Where(b => b.UserId == userId).OrderByDescending(b => b.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).Select(b => MapToResponse(b)).ToListAsync();
        return new PagedResponse<BookingResponse>(items, total, page, pageSize);
    }

    public async Task<bool> ConfirmBookingAsync(Guid bookingId, string paymentReference)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking is null) return false;
        booking.Status = BookingStatus.Confirmed;
        booking.PaymentReference = paymentReference;
        booking.ConfirmedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);
        if (booking is null || booking.Status == BookingStatus.Confirmed) return false;
        booking.Status = BookingStatus.Cancelled;
        // Release seat cache
        var seatCacheKey = $"seat-booked:{booking.EventId}:{booking.SeatId}";
        await _redis.KeyDeleteAsync(seatCacheKey);
        await _db.SaveChangesAsync();
        return true;
    }

    private static BookingResponse MapToResponse(Booking b) =>
        new(b.Id, b.UserId, b.UserEmail, b.EventId, b.EventName, b.EventDate,
            b.SeatNumber, b.Amount, b.Status.ToString(), b.PaymentReference, b.CreatedAt);

    private record EventDetails(Guid Id, string Name, DateTime EventDate, decimal TicketPrice);
    private record ApiResponse<T>(bool Success, T? Data, string? Message);
}
