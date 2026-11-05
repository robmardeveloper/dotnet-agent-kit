using CourtBookings.Domain.Users;

namespace CourtBookings.Application.Bookings;

public sealed record CancelBookingCommand(UserId UserId, int BookingId);
