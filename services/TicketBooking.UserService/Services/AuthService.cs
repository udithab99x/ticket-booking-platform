using Microsoft.AspNetCore.Identity;
using TicketBooking.UserService.DTOs;
using TicketBooking.UserService.Models;

namespace TicketBooking.UserService.Services;

public interface IAuthService
{
    Task<(bool Success, AuthResponse? Response, IEnumerable<string> Errors)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;

    public AuthService(UserManager<AppUser> userManager, IJwtService jwtService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
    }

    public async Task<(bool Success, AuthResponse? Response, IEnumerable<string> Errors)> RegisterAsync(RegisterRequest request)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "Customer"
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, null, result.Errors.Select(e => e.Description));

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var response = new AuthResponse(token, refreshToken, user.Email!, user.FirstName, user.LastName, user.Role, DateTime.UtcNow.AddHours(24));

        return (true, response, []);
    }

    public async Task<(bool Success, AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return (false, null, "Invalid credentials");

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            return (false, null, "Invalid credentials");

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var response = new AuthResponse(token, refreshToken, user.Email!, user.FirstName, user.LastName, user.Role, DateTime.UtcNow.AddHours(24));

        return (true, response, null);
    }
}
