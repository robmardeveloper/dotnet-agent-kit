using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CourtBookings.UnitTests.Application;

public sealed class CancelBookingHandlerTests
{
    private const int CourtId = 1;
    private const int UnknownBookingId = 99;
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBookingRepository bookings = new();
    private readonly FakeTimeProvider timeProvider = new(Now);

    [Fact]
    public async Task HandleAsync_ExistingBooking_CancelsAndSavesIt()
    {
        var booking = Booking.Create(TestUsers.Alice, CourtId, TimeSlot.Create(Now.AddDays(2), TimeSpan.FromMinutes(60)), [], Now);
        bookings.Saved.Add(booking);
        var handler = CreateHandler();

        await handler.HandleAsync(new CancelBookingCommand(TestUsers.Alice, booking.Id), TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(1, bookings.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_SomeoneElsesBooking_ThrowsBookingNotFoundAndLeavesItConfirmed()
    {
        var booking = Booking.Create(TestUsers.Alice, CourtId, TimeSlot.Create(Now.AddDays(2), TimeSpan.FromMinutes(60)), [], Now);
        bookings.Saved.Add(booking);
        var handler = CreateHandler();

        var exception = await Assert.ThrowsAsync<BookingRuleException>(
            () => handler.HandleAsync(new CancelBookingCommand(TestUsers.Bob, booking.Id), TestContext.Current.CancellationToken));

        Assert.Equal((BookingError.BookingNotFound, BookingStatus.Confirmed, 0), (exception.Error, booking.Status, bookings.UpdateCount));
    }

    [Fact]
    public async Task HandleAsync_UnknownBooking_ThrowsBookingNotFound()
    {
        var handler = CreateHandler();

        var exception = await Assert.ThrowsAsync<BookingRuleException>(
            () => handler.HandleAsync(new CancelBookingCommand(TestUsers.Alice, UnknownBookingId), TestContext.Current.CancellationToken));

        Assert.Equal(BookingError.BookingNotFound, exception.Error);
    }

    private CancelBookingHandler CreateHandler() =>
        new(bookings, timeProvider, NullLogger<CancelBookingHandler>.Instance);
}
