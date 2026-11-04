namespace CourtBookings.Domain.Courts;

public sealed class Court
{
    public Court(int id, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }
}
