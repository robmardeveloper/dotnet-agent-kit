using System.Security.Claims;
using CourtBookings.Api.Users;
using CourtBookings.Application.Bookings;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CourtBookings.Api.Bookings;

/// <summary>
/// The booking endpoints. None is marked anonymous: the fallback policy in Program.cs requires a
/// logged-in user on all of them (SEC-4), and the handlers check the booking is that user's.
/// </summary>
public static class BookingEndpoints
{
    private const string GetBookingRoute = "GetBooking";

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/bookings", BookCourt).RequireAuthorization(UserPolicies.RequiresEmail);
        endpoints.MapGet("/bookings/{id:int}", GetBooking).WithName(GetBookingRoute);
        endpoints.MapPost("/bookings/{id:int}/cancel", CancelBooking);
        return endpoints;
    }

    private static async Task<CreatedAtRoute<BookingResponse>> BookCourt(
        BookCourtRequest request, ClaimsPrincipal user, BookCourtHandler handler, CancellationToken cancellationToken)
    {
        var command = new BookCourtCommand(
            user.GetUserId(), user.GetEmail(), request.CourtId, request.Start, TimeSpan.FromMinutes(request.DurationMinutes));
        var confirmation = await handler.HandleAsync(command, cancellationToken);

        var response = new BookingResponse(confirmation.BookingId, confirmation.CourtId, confirmation.Start, confirmation.End);
        return TypedResults.CreatedAtRoute(response, GetBookingRoute, new { id = confirmation.BookingId });
    }

    private static async Task<Ok<BookingDetailsResponse>> GetBooking(
        int id, ClaimsPrincipal user, GetBookingHandler handler, CancellationToken cancellationToken)
    {
        var details = await handler.HandleAsync(new GetBookingQuery(user.GetUserId(), id), cancellationToken);

        return TypedResults.Ok(new BookingDetailsResponse(
            details.BookingId, details.CourtId, details.Start, details.End, details.Status.ToString(), details.HasCancellationPenalty));
    }

    private static async Task<NoContent> CancelBooking(
        int id, ClaimsPrincipal user, CancelBookingHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new CancelBookingCommand(user.GetUserId(), id), cancellationToken);
        return TypedResults.NoContent();
    }
}
