using System.Net;
using System.Net.Http.Json;
using CourtBookings.Api.Bookings;
using Xunit;

namespace CourtBookings.IntegrationTests;

/// <summary>Who may call what (SEC-4): only logged-in users, and each one only on their own bookings.</summary>
public sealed class AuthorizationEndpointTests : IAsyncDisposable
{
    private static readonly BookCourtRequest TomorrowAtTen = new(1, CourtBookingsApiFactory.Now.AddDays(1).AddHours(1), 60);

    private readonly CourtBookingsApiFactory factory = new();

    [Theory]
    [InlineData("POST", "/bookings")]
    [InlineData("GET", "/bookings/1")]
    [InlineData("POST", "/bookings/1/cancel")]
    public async Task AnyEndpoint_NoUser_Returns401(string method, string url)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(TomorrowAtTen) };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostBookings_UserWithoutEmail_Returns403()
    {
        var client = factory.CreateClientFor("carol");

        var response = await client.PostAsJsonAsync("/bookings", TomorrowAtTen, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBooking_SomeoneElsesBooking_Returns404()
    {
        var booking = await BookAsAliceAsync();

        var response = await factory.CreateClientFor("bob").GetAsync($"/bookings/{booking.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_SomeoneElsesBooking_Returns404AndLeavesItConfirmed()
    {
        var booking = await BookAsAliceAsync();

        var response = await factory.CreateClientFor("bob").PostAsync($"/bookings/{booking.Id}/cancel", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var details = await factory.CreateClientForAlice().GetFromJsonAsync<BookingDetailsResponse>(
            $"/bookings/{booking.Id}", TestContext.Current.CancellationToken);
        Assert.Equal("Confirmed", details?.Status);
    }

    public ValueTask DisposeAsync() => factory.DisposeAsync();

    private async Task<BookingResponse> BookAsAliceAsync()
    {
        var response = await factory.CreateClientForAlice().PostAsJsonAsync("/bookings", TomorrowAtTen, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookingResponse>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty booking.");
    }
}
