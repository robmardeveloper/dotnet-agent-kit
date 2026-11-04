using CourtBookings.Application.Bookings;
using Microsoft.Extensions.DependencyInjection;

namespace CourtBookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<BookCourtHandler>();
        return services;
    }
}
