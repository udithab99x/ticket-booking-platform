using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TicketBooking.EventService.Data;
using TicketBooking.EventService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EventDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("EventDb")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddControllers();
builder.Services.AddCors(opts => opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    db.Database.EnsureCreated();

    // Seed sample events if none exist
    if (!db.Events.Any())
    {
        var adminId = new Guid("00000000-0000-0000-0000-000000000001");
        var events = new[]
        {
            new TicketBooking.EventService.Models.Event { Name = "Rock Revolution 2026", Description = "A night of classic rock with top bands.", Category = "Music", Venue = "City Arena", City = "Colombo", EventDate = new DateTime(2026, 9, 15, 19, 0, 0, DateTimeKind.Utc), TotalSeats = 200, AvailableSeats = 200, TicketPrice = 45.00m, CreatedBy = adminId },
            new TicketBooking.EventService.Models.Event { Name = "Premier League Clash", Description = "Live screening of the biggest football match.", Category = "Sports", Venue = "Sports Hub", City = "Kandy", EventDate = new DateTime(2026, 10, 5, 15, 30, 0, DateTimeKind.Utc), TotalSeats = 500, AvailableSeats = 500, TicketPrice = 25.00m, CreatedBy = adminId },
            new TicketBooking.EventService.Models.Event { Name = "Comedy Night Out", Description = "Stand-up comedy featuring top local comedians.", Category = "Comedy", Venue = "Laughs Lounge", City = "Galle", EventDate = new DateTime(2026, 8, 20, 20, 0, 0, DateTimeKind.Utc), TotalSeats = 150, AvailableSeats = 150, TicketPrice = 30.00m, CreatedBy = adminId },
            new TicketBooking.EventService.Models.Event { Name = "Tech Summit 2026", Description = "Annual conference on cloud and AI innovation.", Category = "Conference", Venue = "Convention Centre", City = "Colombo", EventDate = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc), TotalSeats = 1000, AvailableSeats = 1000, TicketPrice = 75.00m, CreatedBy = adminId },
            new TicketBooking.EventService.Models.Event { Name = "Classical Symphony Evening", Description = "An evening of Beethoven and Mozart.", Category = "Music", Venue = "Grand Hall", City = "Colombo", EventDate = new DateTime(2026, 12, 1, 18, 0, 0, DateTimeKind.Utc), TotalSeats = 300, AvailableSeats = 300, TicketPrice = 60.00m, CreatedBy = adminId },
            new TicketBooking.EventService.Models.Event { Name = "Galle Literary Festival", Description = "Celebrate literature with local and international authors.", Category = "Festival", Venue = "Galle Fort", City = "Galle", EventDate = new DateTime(2027, 1, 22, 10, 0, 0, DateTimeKind.Utc), TotalSeats = 800, AvailableSeats = 800, TicketPrice = 20.00m, CreatedBy = adminId },
        };
        db.Events.AddRange(events);
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "EventService" }));

app.Run();
