using CourtBookings.Api.Resources;
using CourtBookings.Domain.Bookings;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace CourtBookings.Api.Bookings;

/// <summary>Turns a refused booking into a problem response with the reason in the user's language (SEC-5, SEP-3).</summary>
internal sealed class BookingRuleExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IStringLocalizer<BookingErrors> localizer) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BookingRuleException ruleException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodeFor(ruleException.Error);
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = httpContext.Response.StatusCode,
                Title = localizer[ruleException.Error.ToString()],
            },
        });
    }

    private static int StatusCodeFor(BookingError error) => error switch
    {
        BookingError.CourtNotFound or BookingError.BookingNotFound => StatusCodes.Status404NotFound,
        BookingError.SlotAlreadyTaken or BookingError.AlreadyCancelled or BookingError.AlreadyStarted => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}
