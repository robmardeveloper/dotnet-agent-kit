using CourtBookings.Application.Courts;

namespace CourtBookings.UnitTests.Application;

internal sealed class InMemoryCourtRepository(params int[] courtIds) : ICourtRepository
{
    public Task<bool> ExistsAsync(int courtId, CancellationToken cancellationToken) =>
        Task.FromResult(courtIds.Contains(courtId));
}
