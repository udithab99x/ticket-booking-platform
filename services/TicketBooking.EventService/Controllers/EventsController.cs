using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.EventService.DTOs;
using TicketBooking.EventService.Services;
using TicketBooking.Shared.DTOs;

namespace TicketBooking.EventService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService) => _eventService = eventService;

    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] EventSearchQuery query)
    {
        var result = await _eventService.GetEventsAsync(query);
        return Ok(new ApiResponse<PagedResponse<EventResponse>>(true, result, null));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var ev = await _eventService.GetEventByIdAsync(id);
        if (ev is null)
            return NotFound(new ApiResponse<EventResponse>(false, null, "Event not found"));
        return Ok(new ApiResponse<EventResponse>(true, ev, null));
    }

    [HttpGet("{id:guid}/seats")]
    public async Task<IActionResult> GetAvailableSeats(Guid id)
    {
        var seats = await _eventService.GetAvailableSeatsAsync(id);
        return Ok(new ApiResponse<IEnumerable<SeatResponse>>(true, seats, null));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var ev = await _eventService.CreateEventAsync(request, userId);
        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id }, new ApiResponse<EventResponse>(true, ev, "Event created"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        var ev = await _eventService.UpdateEventAsync(id, request);
        if (ev is null)
            return NotFound(new ApiResponse<EventResponse>(false, null, "Event not found"));
        return Ok(new ApiResponse<EventResponse>(true, ev, "Event updated"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var deleted = await _eventService.DeleteEventAsync(id);
        if (!deleted)
            return NotFound();
        return Ok(new ApiResponse<object>(true, null, "Event deleted"));
    }
}
