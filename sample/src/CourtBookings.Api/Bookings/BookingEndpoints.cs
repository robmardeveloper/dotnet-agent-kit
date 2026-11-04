using CourtBookings.Application.Bookings;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CourtBookings.Api.Bookings;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/bookings", BookCourt);
        return endpoints;
    }

    private static async Task<Created<BookingResponse>> BookCourt(
        BookCourtRequest request, BookCourtHandler handler, CancellationToken cancellationToken)
    {
        var command = new BookCourtCommand(request.CourtId, request.Start, TimeSpan.FromMinutes(request.DurationMinutes));
        var confirmation = await handler.HandleAsync(command, cancellationToken);

        var response = new BookingResponse(confirmation.BookingId, confirmation.CourtId, confirmation.Start, confirmation.End);
        return TypedResults.Created((string?)null, response);
    }
}
