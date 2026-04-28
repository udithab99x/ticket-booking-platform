namespace TicketBooking.Shared.DTOs;

public record ApiResponse<T>(bool Success, T? Data, string? Message, IEnumerable<string>? Errors = null);

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
};
