using TicketBooking.NotificationService.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<NotificationConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "NotificationService" }));

app.Run();
