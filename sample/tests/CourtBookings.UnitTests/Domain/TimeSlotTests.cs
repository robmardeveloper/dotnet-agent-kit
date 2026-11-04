using CourtBookings.Domain.Bookings;
using Xunit;

namespace CourtBookings.UnitTests.Domain;

public sealed class TimeSlotTests
{
    private static readonly DateTimeOffset TenOClock = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan OneHour = TimeSpan.FromMinutes(60);

    [Theory]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    public void Create_AllowedDuration_EndsThatLongAfterStart(int minutes)
    {
        var slot = TimeSlot.Create(TenOClock, TimeSpan.FromMinutes(minutes));

        Assert.Equal(TenOClock.AddMinutes(minutes), slot.End);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(75)]
    [InlineData(150)]
    public void Create_DurationNotAllowed_ThrowsDurationNotAllowed(int minutes)
    {
        var exception = Assert.Throws<BookingRuleException>(() => TimeSlot.Create(TenOClock, TimeSpan.FromMinutes(minutes)));

        Assert.Equal(BookingError.DurationNotAllowed, exception.Error);
    }

    [Theory]
    [InlineData(15, 0)]
    [InlineData(45, 0)]
    [InlineData(0, 30)]
    public void Create_StartNotOnHalfHour_ThrowsStartNotOnHalfHour(int minutes, int seconds)
    {
        var start = TenOClock + new TimeSpan(0, minutes, seconds);

        var exception = Assert.Throws<BookingRuleException>(() => TimeSlot.Create(start, OneHour));

        Assert.Equal(BookingError.StartNotOnHalfHour, exception.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(-30)]
    public void Overlaps_SlotsShareTime_ReturnsTrue(int otherStartOffsetMinutes)
    {
        var slot = TimeSlot.Create(TenOClock, OneHour);
        var other = TimeSlot.Create(TenOClock.AddMinutes(otherStartOffsetMinutes), OneHour);

        var overlaps = slot.Overlaps(other);

        Assert.True(overlaps);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(-60)]
    public void Overlaps_OneEndsWhenTheOtherStarts_ReturnsFalse(int otherStartOffsetMinutes)
    {
        var slot = TimeSlot.Create(TenOClock, OneHour);
        var other = TimeSlot.Create(TenOClock.AddMinutes(otherStartOffsetMinutes), OneHour);

        var overlaps = slot.Overlaps(other);

        Assert.False(overlaps);
    }
}
