using TicketBooking.BookingService.Models;

namespace TicketBooking.BookingService.DTOs;

public record CreateBookingRequest(
    Guid EventId,
    Guid SeatId,
    string SeatNumber
);

public record BookingResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    Guid EventId,
    string EventName,
    string EventDate,
    string SeatNumber,
    decimal Amount,
    string Status,
    string? PaymentReference,
    DateTime CreatedAt
);

public record ConfirmBookingRequest(
    string PaymentReference
);
