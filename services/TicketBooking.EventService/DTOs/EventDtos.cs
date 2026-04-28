namespace TicketBooking.EventService.DTOs;

public record CreateEventRequest(
    string Name,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTime EventDate,
    int TotalSeats,
    decimal TicketPrice,
    string ImageUrl
);

public record UpdateEventRequest(
    string? Name,
    string? Description,
    string? Category,
    string? Venue,
    string? City,
    DateTime? EventDate,
    decimal? TicketPrice,
    string? ImageUrl
);

public record EventResponse(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTime EventDate,
    int TotalSeats,
    int AvailableSeats,
    decimal TicketPrice,
    string ImageUrl,
    bool IsActive,
    DateTime CreatedAt
);

public record SeatResponse(
    Guid Id,
    string SeatNumber,
    string Row,
    string Section,
    bool IsBooked
);

public record EventSearchQuery(
    string? Category,
    string? City,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 10
);
