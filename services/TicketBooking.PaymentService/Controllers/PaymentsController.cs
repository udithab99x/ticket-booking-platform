using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.PaymentService.DTOs;
using TicketBooking.PaymentService.Services;
using TicketBooking.Shared.DTOs;

namespace TicketBooking.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var (success, payment, error) = await _paymentService.ProcessPaymentAsync(request, userId);
        if (!success)
            return BadRequest(new ApiResponse<PaymentResponse>(false, null, error));
        return Ok(new ApiResponse<PaymentResponse>(true, payment, "Payment processed successfully"));
    }

    [HttpGet("booking/{bookingId:guid}")]
    public async Task<IActionResult> GetByBooking(Guid bookingId)
    {
        var payment = await _paymentService.GetPaymentByBookingAsync(bookingId);
        if (payment is null)
            return NotFound(new ApiResponse<PaymentResponse>(false, null, "Payment not found"));
        return Ok(new ApiResponse<PaymentResponse>(true, payment, null));
    }
}
