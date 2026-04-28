using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Shared.DTOs;
using TicketBooking.UserService.DTOs;
using TicketBooking.UserService.Models;
using TicketBooking.UserService.Services;

namespace TicketBooking.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, response, errors) = await _authService.RegisterAsync(request);
        if (!success)
            return BadRequest(new ApiResponse<AuthResponse>(false, null, "Registration failed", errors));

        return Ok(new ApiResponse<AuthResponse>(true, response, "Registered successfully"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, response, error) = await _authService.LoginAsync(request);
        if (!success)
            return Unauthorized(new ApiResponse<AuthResponse>(false, null, error));

        return Ok(new ApiResponse<AuthResponse>(true, response, "Login successful"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public UsersController(UserManager<AppUser> userManager) => _userManager = userManager;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        var profile = new UserProfileResponse(user.Id, user.Email!, user.FirstName, user.LastName, user.Role, user.CreatedAt);
        return Ok(new ApiResponse<UserProfileResponse>(true, profile, null));
    }
}
