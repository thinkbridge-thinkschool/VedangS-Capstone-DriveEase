using DriveEase.Students.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Students.Infrastructure.Persistence;

public sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.Property(x => x.Family).HasMaxLength(50).IsRequired();
            e.Property(x => x.ReplacedByToken).HasMaxLength(200);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.StudentId, x.RevokedAt });
            e.Ignore(x => x.IsActive);
        });
    }
}
