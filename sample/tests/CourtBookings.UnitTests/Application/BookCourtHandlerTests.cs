using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CourtBookings.UnitTests.Application;

public sealed class BookCourtHandlerTests
{
    private const int CourtId = 1;
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan OneHour = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan TwoHours = TimeSpan.FromMinutes(120);

    private readonly InMemoryBookingRepository bookings = new();
    private readonly FakeTimeProvider timeProvider = new(Now);

    [Fact]
    public async Task HandleAsync_FreeSlot_SavesBookingAndConfirmsIt()
    {
        var handler = CreateHandler(new InMemoryCourtRepository(CourtId));
        var command = new BookCourtCommand(CourtId, Now.AddHours(1), OneHour);

        var confirmation = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var saved = Assert.Single(bookings.Saved);
        Assert.Equal(new BookingConfirmation(saved.Id, CourtId, command.Start, command.Start + OneHour), confirmation);
    }

    [Fact]
    public async Task HandleAsync_UnknownCourt_ThrowsCourtNotFound()
    {
        var handler = CreateHandler(new InMemoryCourtRepository());
        var command = new BookCourtCommand(CourtId, Now.AddHours(1), OneHour);

        var exception = await Assert.ThrowsAsync<BookingRuleException>(
            () => handler.HandleAsync(command, TestContext.Current.CancellationToken));

        Assert.Equal(BookingError.CourtNotFound, exception.Error);
    }

    [Fact]
    public async Task HandleAsync_CourtInUseByABookingThatAlreadyStarted_ThrowsSlotAlreadyTaken()
    {
        var handler = CreateHandler(new InMemoryCourtRepository(CourtId));
        var earlier = new BookCourtCommand(CourtId, Now.AddHours(1), TwoHours);
        await handler.HandleAsync(earlier, TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromMinutes(75));
        var command = new BookCourtCommand(CourtId, Now.AddHours(2).AddMinutes(30), OneHour);

        var exception = await Assert.ThrowsAsync<BookingRuleException>(
            () => handler.HandleAsync(command, TestContext.Current.CancellationToken));

        Assert.Equal(BookingError.SlotAlreadyTaken, exception.Error);
    }

    [Fact]
    public async Task HandleAsync_SlotOfACancelledBooking_BooksIt()
    {
        var cancelled = Booking.Create(CourtId, TimeSlot.Create(Now.AddHours(1), OneHour), [], Now);
        cancelled.Cancel(Now);
        bookings.Saved.Add(cancelled);
        var handler = CreateHandler(new InMemoryCourtRepository(CourtId));

        await handler.HandleAsync(new BookCourtCommand(CourtId, Now.AddHours(1), OneHour), TestContext.Current.CancellationToken);

        Assert.Equal(2, bookings.Saved.Count);
    }

    private BookCourtHandler CreateHandler(InMemoryCourtRepository courts) =>
        new(courts, bookings, timeProvider, NullLogger<BookCourtHandler>.Instance);
}
