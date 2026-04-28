namespace TicketBooking.Shared.Messages;

public record PaymentCompletedMessage(
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    decimal Amount,
    string Status,
    DateTime ProcessedAt
);
