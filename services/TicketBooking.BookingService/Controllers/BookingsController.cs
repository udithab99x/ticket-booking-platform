using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.BookingService.DTOs;
using TicketBooking.BookingService.Services;
using TicketBooking.Shared.DTOs;

namespace TicketBooking.BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService) => _bookingService = bookingService;

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var email = User.FindFirst("email")?.Value ?? "";

        var (success, booking, error) = await _bookingService.CreateBookingAsync(request, userId, email);
        if (!success)
            return Conflict(new ApiResponse<BookingResponse>(false, null, error));

        return CreatedAtAction(nameof(GetBooking), new { id = booking!.Id },
            new ApiResponse<BookingResponse>(true, booking, "Booking created. Proceed to payment."));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var booking = await _bookingService.GetBookingAsync(id, userId);
        if (booking is null)
            return NotFound(new ApiResponse<BookingResponse>(false, null, "Booking not found"));
        return Ok(new ApiResponse<BookingResponse>(true, booking, null));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var result = await _bookingService.GetUserBookingsAsync(userId, page, pageSize);
        return Ok(new ApiResponse<PagedResponse<BookingResponse>>(true, result, null));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var cancelled = await _bookingService.CancelBookingAsync(id, userId);
        if (!cancelled)
            return BadRequest(new ApiResponse<object>(false, null, "Cannot cancel this booking"));
        return Ok(new ApiResponse<object>(true, null, "Booking cancelled"));
    }

    // Internal endpoint called by Payment Service
    [HttpPost("{id:guid}/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmBooking(Guid id, [FromBody] ConfirmBookingRequest request)
    {
        var confirmed = await _bookingService.ConfirmBookingAsync(id, request.PaymentReference);
        if (!confirmed)
            return NotFound();
        return Ok(new ApiResponse<object>(true, null, "Booking confirmed"));
    }
}
