using Microsoft.EntityFrameworkCore;
using TicketBooking.EventService.Models;

namespace TicketBooking.EventService.Data;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("events");

        b.Entity<Event>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Venue).HasMaxLength(300);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.TicketPrice).HasPrecision(18, 2);
            e.HasMany(x => x.Seats).WithOne(s => s.Event).HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Seat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SeatNumber).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.EventId, x.SeatNumber }).IsUnique();
        });
    }
}
