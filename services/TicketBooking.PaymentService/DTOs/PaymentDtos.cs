namespace TicketBooking.PaymentService.DTOs;

public record ProcessPaymentRequest(
    Guid BookingId,
    decimal Amount,
    string CardLastFour,
    string CardHolderName
);

public record PaymentResponse(
    Guid Id,
    Guid BookingId,
    decimal Amount,
    string Status,
    string? TransactionReference,
    DateTime CreatedAt
);
