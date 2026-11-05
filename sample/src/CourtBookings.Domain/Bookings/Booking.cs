namespace CourtBookings.Domain.Bookings;

public sealed class Booking
{
    public static readonly TimeSpan MaxBookingAhead = TimeSpan.FromDays(7);

    public static readonly TimeSpan FreeCancellationNotice = TimeSpan.FromHours(24);

    private Booking(int courtId, TimeSlot slot)
    {
        CourtId = courtId;
        Slot = slot;
    }

#pragma warning disable CS8618 // EF Core needs a constructor without the slot: it sets Slot itself when it loads a booking.
    private Booking()
    {
    }
#pragma warning restore CS8618

    public int Id { get; private set; }

    public int CourtId { get; private set; }

    public TimeSlot Slot { get; private set; }

    public BookingStatus Status { get; private set; } = BookingStatus.Confirmed;

    public bool HasCancellationPenalty { get; private set; }

    /// <summary>
    /// Books a court. The court's other bookings are passed in because the "no overlap" rule
    /// spans bookings, and the time is passed in so the domain never reads the clock (DI-2).
    /// </summary>
    public static Booking Create(int courtId, TimeSlot slot, IEnumerable<TimeSlot> takenSlots, DateTimeOffset now)
    {
        if (slot.Start <= now)
        {
            throw new BookingRuleException(BookingError.StartsInThePast);
        }

        if (slot.Start > now + MaxBookingAhead)
        {
            throw new BookingRuleException(BookingError.TooFarAhead);
        }

        if (takenSlots.Any(slot.Overlaps))
        {
            throw new BookingRuleException(BookingError.SlotAlreadyTaken);
        }

        return new Booking(courtId, slot);
    }

    /// <summary>
    /// Cancels the booking: free until <see cref="FreeCancellationNotice"/> before it starts,
    /// with a penalty after that, and not possible once it has started.
    /// </summary>
    public void Cancel(DateTimeOffset now)
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new BookingRuleException(BookingError.AlreadyCancelled);
        }

        if (now >= Slot.Start)
        {
            throw new BookingRuleException(BookingError.AlreadyStarted);
        }

        Status = BookingStatus.Cancelled;
        HasCancellationPenalty = Slot.Start - now < FreeCancellationNotice;
    }
}
