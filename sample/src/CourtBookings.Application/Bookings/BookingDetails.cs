using CourtBookings.Domain.Bookings;

namespace CourtBookings.Application.Bookings;

public sealed record BookingDetails(
    int BookingId,
    int CourtId,
    DateTimeOffset Start,
    DateTimeOffset End,
    BookingStatus Status,
    bool HasCancellationPenalty);
