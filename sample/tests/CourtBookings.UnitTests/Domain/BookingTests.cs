using CourtBookings.Domain.Bookings;
using Xunit;

namespace CourtBookings.UnitTests.Domain;

public sealed class BookingTests
{
    private const int CourtId = 1;
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan OneHour = TimeSpan.FromMinutes(60);

    [Fact]
    public void Create_FreeSlotWithinSevenDays_CreatesBookingForThatSlot()
    {
        var slot = TimeSlot.Create(Now.AddHours(1), OneHour);

        var booking = Booking.Create(CourtId, slot, [], Now);

        Assert.Equal(CourtId, booking.CourtId);
        Assert.Equal(slot, booking.Slot);
    }

    [Fact]
    public void Create_ExactlySevenDaysAhead_CreatesBooking()
    {
        var slot = TimeSlot.Create(Now.AddDays(7), OneHour);

        var booking = Booking.Create(CourtId, slot, [], Now);

        Assert.Equal(slot, booking.Slot);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_StartNotInTheFuture_ThrowsStartsInThePast(int hoursFromNow)
    {
        var slot = TimeSlot.Create(Now.AddHours(hoursFromNow), OneHour);

        var exception = Assert.Throws<BookingRuleException>(() => Booking.Create(CourtId, slot, [], Now));

        Assert.Equal(BookingError.StartsInThePast, exception.Error);
    }

    [Fact]
    public void Create_MoreThanSevenDaysAhead_ThrowsTooFarAhead()
    {
        var slot = TimeSlot.Create(Now.AddDays(7).AddMinutes(30), OneHour);

        var exception = Assert.Throws<BookingRuleException>(() => Booking.Create(CourtId, slot, [], Now));

        Assert.Equal(BookingError.TooFarAhead, exception.Error);
    }

    [Fact]
    public void Create_OverlapsATakenSlot_ThrowsSlotAlreadyTaken()
    {
        var taken = TimeSlot.Create(Now.AddHours(1), TimeSpan.FromMinutes(90));
        var slot = TimeSlot.Create(Now.AddHours(2), OneHour);

        var exception = Assert.Throws<BookingRuleException>(() => Booking.Create(CourtId, slot, [taken], Now));

        Assert.Equal(BookingError.SlotAlreadyTaken, exception.Error);
    }

    [Theory]
    [InlineData(48)]
    [InlineData(24)]
    public void Cancel_AtLeast24HoursBefore_CancelsWithoutPenalty(int hoursBeforeStart)
    {
        var booking = BookingStartingAt(Now.AddDays(3));

        booking.Cancel(booking.Slot.Start.AddHours(-hoursBeforeStart));

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.False(booking.HasCancellationPenalty);
    }

    [Theory]
    [InlineData(23.5)]
    [InlineData(0.5)]
    public void Cancel_LessThan24HoursBefore_ChargesPenalty(double hoursBeforeStart)
    {
        var booking = BookingStartingAt(Now.AddDays(3));

        booking.Cancel(booking.Slot.Start.AddHours(-hoursBeforeStart));

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.True(booking.HasCancellationPenalty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void Cancel_BookingAlreadyStarted_ThrowsAlreadyStarted(int minutesAfterStart)
    {
        var booking = BookingStartingAt(Now.AddDays(3));

        var exception = Assert.Throws<BookingRuleException>(() => booking.Cancel(booking.Slot.Start.AddMinutes(minutesAfterStart)));

        Assert.Equal(BookingError.AlreadyStarted, exception.Error);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsAlreadyCancelled()
    {
        var booking = BookingStartingAt(Now.AddDays(3));
        booking.Cancel(Now);

        var exception = Assert.Throws<BookingRuleException>(() => booking.Cancel(Now));

        Assert.Equal(BookingError.AlreadyCancelled, exception.Error);
    }

    private static Booking BookingStartingAt(DateTimeOffset start) =>
        Booking.Create(CourtId, TimeSlot.Create(start, OneHour), [], Now);
}
