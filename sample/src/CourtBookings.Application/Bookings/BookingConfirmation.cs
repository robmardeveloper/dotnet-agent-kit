namespace CourtBookings.Application.Bookings;

public sealed record BookingConfirmation(int BookingId, int CourtId, DateTimeOffset Start, DateTimeOffset End);
