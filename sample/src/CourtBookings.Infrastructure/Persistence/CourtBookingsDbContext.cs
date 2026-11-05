using CourtBookings.Domain.Bookings;
using CourtBookings.Domain.Courts;
using CourtBookings.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CourtBookings.Infrastructure.Persistence;

public sealed class CourtBookingsDbContext(DbContextOptions<CourtBookingsDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Court> Courts => Set<Court>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no type for DateTimeOffset and cannot compare it in a query; stored as a
        // number, the comparison in ListConfirmedSlotsEndingAfterAsync runs in the database.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(booking =>
        {
            booking.HasKey(b => b.Id);
            booking.Property(b => b.OwnerId)
                .HasConversion(id => id.Value, value => UserId.Create(value))
                .HasMaxLength(UserId.MaxLength);
            booking.HasOne<Court>().WithMany().HasForeignKey(b => b.CourtId);
            booking.ComplexProperty(b => b.Slot);
            booking.Property(b => b.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
        });

        modelBuilder.Entity<Court>(court =>
        {
            court.HasKey(c => c.Id);
            court.Property(c => c.Id).ValueGeneratedNever();
            court.Property(c => c.Name).HasMaxLength(CourtNameMaxLength);
            court.HasData(new Court(1, "Court 1"), new Court(2, "Court 2"));
        });
    }

    private const int CourtNameMaxLength = 50;
    private const int StatusMaxLength = 20;
}
