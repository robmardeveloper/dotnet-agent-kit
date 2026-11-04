using System.Net;
using System.Net.Http.Json;
using CourtBookings.Api.Bookings;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CourtBookings.IntegrationTests;

public sealed class BookCourtEndpointTests : IAsyncDisposable
{
    private const string BookingsUrl = "/bookings";
    private static readonly DateTimeOffset TenOClock = CourtBookingsApiFactory.Now.AddHours(1);

    private readonly CourtBookingsApiFactory factory = new();

    [Fact]
    public async Task PostBookings_FreeSlot_Returns201WithTheBooking()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(BookingsUrl, new BookCourtRequest(1, TenOClock, 90), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(new BookingResponse(1, 1, TenOClock, TenOClock.AddMinutes(90)), booking);
    }

    [Fact]
    public async Task PostBookings_SlotAlreadyTaken_Returns409()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(BookingsUrl, new BookCourtRequest(1, TenOClock, 90), TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync(BookingsUrl, new BookCourtRequest(1, TenOClock.AddMinutes(30), 60), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostBookings_UnknownCourt_Returns404()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(BookingsUrl, new BookCourtRequest(99, TenOClock, 60), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(1, 30)]
    [InlineData(1, 180)]
    public async Task PostBookings_InputOutOfRange_Returns400WithTheField(int courtId, int durationMinutes)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(BookingsUrl, new BookCourtRequest(courtId, TenOClock, durationMinutes), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotEmpty(problem?.Errors ?? new Dictionary<string, string[]>());
    }

    [Fact]
    public async Task PostBookings_SpanishClient_ReturnsTheReasonInSpanish()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es");

        var response = await client.PostAsJsonAsync(BookingsUrl, new BookCourtRequest(1, TenOClock.AddDays(8), 60), TestContext.Current.CancellationToken);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal("Solo se puede reservar con 7 días de antelación como máximo.", problem?.Title);
    }

    public ValueTask DisposeAsync() => factory.DisposeAsync();
}
