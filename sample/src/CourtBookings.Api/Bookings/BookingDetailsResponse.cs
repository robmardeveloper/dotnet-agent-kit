namespace CourtBookings.Api.Bookings;

public sealed record BookingDetailsResponse(
    int Id,
    int CourtId,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Status,
    bool HasCancellationPenalty);
