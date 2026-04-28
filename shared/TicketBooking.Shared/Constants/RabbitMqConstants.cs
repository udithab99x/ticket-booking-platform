namespace TicketBooking.Shared.Constants;

public static class RabbitMqConstants
{
    public const string BookingExchange = "booking.exchange";
    public const string PaymentExchange = "payment.exchange";

    public const string BookingCreatedQueue = "booking.created";
    public const string PaymentCompletedQueue = "payment.completed";

    public const string BookingCreatedRoutingKey = "booking.created";
    public const string PaymentCompletedRoutingKey = "payment.completed";
}
