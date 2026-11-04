using CourtBookings.Application.Bookings;
using CourtBookings.Application.Courts;
using CourtBookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourtBookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CourtBookingsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<ICourtRepository, EfCourtRepository>();
        return services;
    }
}
