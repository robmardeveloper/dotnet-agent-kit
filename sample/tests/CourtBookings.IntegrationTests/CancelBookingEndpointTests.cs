using System.Net;
using System.Net.Http.Json;
using CourtBookings.Api.Bookings;
using Xunit;

namespace CourtBookings.IntegrationTests;

public sealed class CancelBookingEndpointTests : IAsyncDisposable
{
    private const int CourtId = 1;
    private const int UnknownBookingId = 99;
    private static readonly DateTimeOffset InTwoDays = CourtBookingsApiFactory.Now.AddDays(2);
    private static readonly DateTimeOffset InOneHour = CourtBookingsApiFactory.Now.AddHours(1);

    private readonly CourtBookingsApiFactory factory = new();
    private readonly HttpClient client;

    public CancelBookingEndpointTests()
    {
        client = factory.CreateClientForAlice();
    }

    [Fact]
    public async Task CancelBooking_MoreThan24HoursBefore_CancelsWithoutPenalty()
    {
        var booking = await BookAsync(InTwoDays);

        var response = await client.PostAsync(CancelUrl(booking.Id), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await GetAsync(booking.Id);
        Assert.Equal(("Cancelled", false), (details.Status, details.HasCancellationPenalty));
    }

    [Fact]
    public async Task CancelBooking_LessThan24HoursBefore_ChargesPenalty()
    {
        var booking = await BookAsync(InOneHour);

        var response = await client.PostAsync(CancelUrl(booking.Id), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await GetAsync(booking.Id);
        Assert.Equal(("Cancelled", true), (details.Status, details.HasCancellationPenalty));
    }

    [Fact]
    public async Task CancelBooking_AlreadyCancelled_Returns409()
    {
        var booking = await BookAsync(InTwoDays);
        await client.PostAsync(CancelUrl(booking.Id), null, TestContext.Current.CancellationToken);

        var response = await client.PostAsync(CancelUrl(booking.Id), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_UnknownBooking_Returns404()
    {
        var response = await client.PostAsync(CancelUrl(UnknownBookingId), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostBookings_SlotOfACancelledBooking_Returns201()
    {
        var booking = await BookAsync(InTwoDays);
        await client.PostAsync(CancelUrl(booking.Id), null, TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync("/bookings", new BookCourtRequest(CourtId, InTwoDays, 60), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
    }

    private static string CancelUrl(int bookingId) => $"/bookings/{bookingId}/cancel";

    private async Task<BookingResponse> BookAsync(DateTimeOffset start)
    {
        var response = await client.PostAsJsonAsync("/bookings", new BookCourtRequest(CourtId, start, 60), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookingResponse>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty booking.");
    }

    private async Task<BookingDetailsResponse> GetAsync(int bookingId) =>
        await client.GetFromJsonAsync<BookingDetailsResponse>($"/bookings/{bookingId}", TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty booking.");
}
