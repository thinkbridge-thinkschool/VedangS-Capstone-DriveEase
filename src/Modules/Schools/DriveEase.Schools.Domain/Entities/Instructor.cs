using DriveEase.Shared.Domain;

namespace DriveEase.Schools.Domain.Entities;

public sealed class Instructor : Entity<Guid>
{
    public Guid SchoolId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public bool IsAvailable { get; private set; }

    private Instructor() { }

    public static Instructor Create(Guid schoolId, string fullName, string licenseNumber) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            FullName = fullName,
            LicenseNumber = licenseNumber,
            IsAvailable = true
        };

    public void SetAvailability(bool available) => IsAvailable = available;
}
