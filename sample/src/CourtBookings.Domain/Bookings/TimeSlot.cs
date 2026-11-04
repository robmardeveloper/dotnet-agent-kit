namespace CourtBookings.Domain.Bookings;

/// <summary>The time a court is booked for: a start on the hour or half hour and an allowed length (DOM-3).</summary>
public sealed record TimeSlot
{
    private const int MinutesPerStep = 30;

    private static readonly TimeSpan[] AllowedDurations =
    [
        TimeSpan.FromMinutes(60),
        TimeSpan.FromMinutes(90),
        TimeSpan.FromMinutes(120),
    ];

    private TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; private init; }

    public DateTimeOffset End { get; private init; }

    public static TimeSlot Create(DateTimeOffset start, TimeSpan duration)
    {
        if (!AllowedDurations.Contains(duration))
        {
            throw new BookingRuleException(BookingError.DurationNotAllowed);
        }

        if (!IsOnHalfHour(start))
        {
            throw new BookingRuleException(BookingError.StartNotOnHalfHour);
        }

        return new TimeSlot(start, start + duration);
    }

    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;

    private static bool IsOnHalfHour(DateTimeOffset start) =>
        start.Minute % MinutesPerStep == 0 && start.Second == 0 && start.Millisecond == 0 && start.Microsecond == 0;
}
