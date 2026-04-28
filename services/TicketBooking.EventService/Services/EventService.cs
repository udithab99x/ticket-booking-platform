using Microsoft.EntityFrameworkCore;
using TicketBooking.EventService.Data;
using TicketBooking.EventService.DTOs;
using TicketBooking.EventService.Models;
using TicketBooking.Shared.DTOs;

namespace TicketBooking.EventService.Services;

public interface IEventService
{
    Task<PagedResponse<EventResponse>> GetEventsAsync(EventSearchQuery query);
    Task<EventResponse?> GetEventByIdAsync(Guid id);
    Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid userId);
    Task<EventResponse?> UpdateEventAsync(Guid id, UpdateEventRequest request);
    Task<bool> DeleteEventAsync(Guid id);
    Task<IEnumerable<SeatResponse>> GetAvailableSeatsAsync(Guid eventId);
}

public class EventService : IEventService
{
    private readonly EventDbContext _db;

    public EventService(EventDbContext db) => _db = db;

    public async Task<PagedResponse<EventResponse>> GetEventsAsync(EventSearchQuery query)
    {
        var q = _db.Events.Where(e => e.IsActive && e.EventDate > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(e => e.Category == query.Category);
        if (!string.IsNullOrWhiteSpace(query.City))
            q = q.Where(e => e.City.Contains(query.City));
        if (query.DateFrom.HasValue)
            q = q.Where(e => e.EventDate >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            q = q.Where(e => e.EventDate <= query.DateTo.Value);

        var total = await q.CountAsync();
        var items = await q.OrderBy(e => e.EventDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => MapToResponse(e))
            .ToListAsync();

        return new PagedResponse<EventResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<EventResponse?> GetEventByIdAsync(Guid id)
    {
        var ev = await _db.Events.FindAsync(id);
        return ev is null ? null : MapToResponse(ev);
    }

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid userId)
    {
        var ev = new Event
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Venue = request.Venue,
            City = request.City,
            EventDate = request.EventDate,
            TotalSeats = request.TotalSeats,
            AvailableSeats = request.TotalSeats,
            TicketPrice = request.TicketPrice,
            ImageUrl = request.ImageUrl,
            CreatedBy = userId
        };

        // Generate seats
        var seats = new List<Seat>();
        int rows = (int)Math.Ceiling(request.TotalSeats / 10.0);
        int seatNum = 1;
        for (int r = 0; r < rows && seatNum <= request.TotalSeats; r++)
        {
            string rowLetter = ((char)('A' + r)).ToString();
            for (int s = 1; s <= 10 && seatNum <= request.TotalSeats; s++, seatNum++)
            {
                seats.Add(new Seat
                {
                    EventId = ev.Id,
                    SeatNumber = $"{rowLetter}{s}",
                    Row = rowLetter,
                    Section = r < rows / 2 ? "Front" : "Back"
                });
            }
        }

        ev.Seats = seats;
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return MapToResponse(ev);
    }

    public async Task<EventResponse?> UpdateEventAsync(Guid id, UpdateEventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return null;

        if (request.Name is not null) ev.Name = request.Name;
        if (request.Description is not null) ev.Description = request.Description;
        if (request.Category is not null) ev.Category = request.Category;
        if (request.Venue is not null) ev.Venue = request.Venue;
        if (request.City is not null) ev.City = request.City;
        if (request.EventDate.HasValue) ev.EventDate = request.EventDate.Value;
        if (request.TicketPrice.HasValue) ev.TicketPrice = request.TicketPrice.Value;
        if (request.ImageUrl is not null) ev.ImageUrl = request.ImageUrl;

        await _db.SaveChangesAsync();
        return MapToResponse(ev);
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return false;
        ev.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SeatResponse>> GetAvailableSeatsAsync(Guid eventId)
    {
        return await _db.Seats
            .Where(s => s.EventId == eventId && !s.IsBooked)
            .Select(s => new SeatResponse(s.Id, s.SeatNumber, s.Row, s.Section, s.IsBooked))
            .ToListAsync();
    }

    private static EventResponse MapToResponse(Event e) =>
        new(e.Id, e.Name, e.Description, e.Category, e.Venue, e.City,
            e.EventDate, e.TotalSeats, e.AvailableSeats, e.TicketPrice,
            e.ImageUrl, e.IsActive, e.CreatedAt);
}
