namespace CourtBookings.Application.Courts;

public interface ICourtRepository
{
    Task<bool> ExistsAsync(int courtId, CancellationToken cancellationToken);
}
