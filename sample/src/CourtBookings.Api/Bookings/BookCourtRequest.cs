using System.ComponentModel.DataAnnotations;

namespace CourtBookings.Api.Bookings;

public sealed record BookCourtRequest(
    [property: Range(1, int.MaxValue)] int CourtId,
    DateTimeOffset Start,
    [property: Range(BookCourtRequest.MinDurationMinutes, BookCourtRequest.MaxDurationMinutes)] int DurationMinutes)
{
    // A first filter at the boundary (SEC-3); the exact allowed lengths are a domain rule (TimeSlot).
    public const int MinDurationMinutes = 60;
    public const int MaxDurationMinutes = 120;
}
