namespace TicketBooking.Shared.Messages;

public record BookingCreatedMessage(
    Guid BookingId,
    Guid UserId,
    string UserEmail,
    Guid EventId,
    string EventName,
    string EventDate,
    string SeatNumber,
    decimal Amount,
    DateTime CreatedAt
);
