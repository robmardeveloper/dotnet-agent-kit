using CourtBookings.Domain.Users;

namespace CourtBookings.Application.Bookings;

public sealed record BookCourtCommand(UserId UserId, EmailAddress Email, int CourtId, DateTimeOffset Start, TimeSpan Duration);
