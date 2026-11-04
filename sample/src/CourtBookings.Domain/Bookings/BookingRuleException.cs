namespace CourtBookings.Domain.Bookings;

public sealed class BookingRuleException : Exception
{
    public BookingRuleException(BookingError error)
        : base($"Booking refused: {error}.")
    {
        Error = error;
    }

    public BookingError Error { get; }
}
