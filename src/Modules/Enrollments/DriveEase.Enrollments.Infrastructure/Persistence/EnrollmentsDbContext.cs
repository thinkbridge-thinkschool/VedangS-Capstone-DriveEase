using DriveEase.Enrollments.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Enrollments.Infrastructure.Persistence;

public sealed class EnrollmentsDbContext(DbContextOptions<EnrollmentsDbContext> options) : DbContext(options)
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("enrollments");

        modelBuilder.Entity<Enrollment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StudentId).IsRequired();
            e.Property(x => x.DrivingSchoolId).IsRequired();
            e.Property(x => x.InstructorId);
            e.Property(x => x.Fee).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.PaymentStatus).HasConversion<string>().IsRequired();
            e.Property(x => x.Status).HasConversion<string>().IsRequired();
            e.Property(x => x.EnrolledAt).IsRequired();
            e.Property(x => x.PaymentConfirmedAt);
            e.Property(x => x.CancelledAt);

            e.Ignore(x => x.DomainEvents);

            e.HasIndex(x => x.StudentId);
            e.HasIndex(x => new { x.StudentId, x.Status });
        });
    }
}
