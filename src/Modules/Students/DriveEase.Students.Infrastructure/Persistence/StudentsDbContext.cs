using DriveEase.Students.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Students.Infrastructure.Persistence;

public sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("students");

        modelBuilder.Entity<Student>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.DateOfBirth).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Ignore(x => x.DomainEvents);
        });
    }
}
