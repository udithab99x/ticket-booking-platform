namespace TicketBooking.UserService.DTOs;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string RefreshToken,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime ExpiresAt
);

public record UserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime CreatedAt
);
