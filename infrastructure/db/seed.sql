-- Seed sample events for the Event Ticket Booking Platform

INSERT INTO events."Events" ("Id", "Name", "Description", "Category", "Venue", "City", "EventDate", "TotalSeats", "AvailableSeats", "TicketPrice", "ImageUrl", "IsActive", "CreatedBy", "CreatedAt")
VALUES
  (gen_random_uuid(), 'Rock Revolution 2026', 'A night of classic rock with top bands.', 'Music', 'City Arena', 'Colombo', '2026-09-15 19:00:00', 200, 200, 45.00, NULL, true, '00000000-0000-0000-0000-000000000001', NOW()),
  (gen_random_uuid(), 'Premier League Clash', 'Live screening of the biggest football match.', 'Sports', 'Sports Hub', 'Kandy', '2026-10-05 15:30:00', 500, 500, 25.00, NULL, true, '00000000-0000-0000-0000-000000000001', NOW()),
  (gen_random_uuid(), 'Comedy Night Out', 'Stand-up comedy featuring top local comedians.', 'Comedy', 'Laughs Lounge', 'Galle', '2026-08-20 20:00:00', 150, 150, 30.00, NULL, true, '00000000-0000-0000-0000-000000000001', NOW()),
  (gen_random_uuid(), 'Tech Summit 2026', 'Annual conference on cloud and AI innovation.', 'Conference', 'Convention Centre', 'Colombo', '2026-11-10 09:00:00', 1000, 1000, 75.00, NULL, true, '00000000-0000-0000-0000-000000000001', NOW()),
  (gen_random_uuid(), 'Classical Symphony Evening', 'An evening of Beethoven and Mozart.', 'Music', 'Grand Hall', 'Colombo', '2026-12-01 18:00:00', 300, 300, 60.00, NULL, true, '00000000-0000-0000-0000-000000000001', NOW()),
  (gen_random_uuid(), 'Galle Literary Festival', 'Celebrate literature with local and international authors.', 'Festival', 'Galle Fort', 'Galle', '2027-01-22 10:00:00', 800, 800, 20.00, NULL, true, '00000000-0000-0000-0000-000000000001', NOW());
