namespace CourtBookings.Application.Bookings;

public sealed record BookCourtCommand(int CourtId, DateTimeOffset Start, TimeSpan Duration);
