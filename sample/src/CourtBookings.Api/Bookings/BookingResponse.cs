namespace CourtBookings.Api.Bookings;

public sealed record BookingResponse(int Id, int CourtId, DateTimeOffset Start, DateTimeOffset End);
