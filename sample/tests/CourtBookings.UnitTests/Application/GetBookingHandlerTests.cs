using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;
using Xunit;

namespace CourtBookings.UnitTests.Application;

public sealed class GetBookingHandlerTests
{
    private const int CourtId = 1;
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBookingRepository bookings = new();
    private readonly Booking booking = Booking.Create(TestUsers.Alice, CourtId, TimeSlot.Create(Now.AddDays(2), TimeSpan.FromMinutes(60)), [], Now);

    public GetBookingHandlerTests()
    {
        bookings.Saved.Add(booking);
    }

    [Fact]
    public async Task HandleAsync_OwnBooking_ReturnsIt()
    {
        var details = await new GetBookingHandler(bookings).HandleAsync(
            new GetBookingQuery(TestUsers.Alice, booking.Id), TestContext.Current.CancellationToken);

        Assert.Equal(booking.Slot.Start, details.Start);
    }

    [Fact]
    public async Task HandleAsync_SomeoneElsesBooking_ThrowsBookingNotFound()
    {
        var exception = await Assert.ThrowsAsync<BookingRuleException>(() => new GetBookingHandler(bookings).HandleAsync(
            new GetBookingQuery(TestUsers.Bob, booking.Id), TestContext.Current.CancellationToken));

        Assert.Equal(BookingError.BookingNotFound, exception.Error);
    }
}
