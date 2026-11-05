using CourtBookings.Domain.Users;

namespace CourtBookings.Application.Bookings;

public sealed record GetBookingQuery(UserId UserId, int BookingId);
