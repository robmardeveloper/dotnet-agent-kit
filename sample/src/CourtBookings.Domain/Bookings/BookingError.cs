namespace CourtBookings.Domain.Bookings;

/// <summary>Why a booking operation was refused. Each value has its user text in the API's resource files (SEP-3).</summary>
public enum BookingError
{
    CourtNotFound,
    DurationNotAllowed,
    StartNotOnHalfHour,
    StartsInThePast,
    TooFarAhead,
    SlotAlreadyTaken,
    BookingNotFound,
    AlreadyCancelled,
    AlreadyStarted,
}
