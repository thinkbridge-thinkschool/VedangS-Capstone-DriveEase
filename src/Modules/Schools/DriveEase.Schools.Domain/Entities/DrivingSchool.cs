using DriveEase.Shared.Domain;

namespace DriveEase.Schools.Domain.Entities;

public sealed class DrivingSchool : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime RegisteredAt { get; private set; }

    private DrivingSchool() { }

    public static DrivingSchool Register(string name, string address, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("School name is required.");

        return new DrivingSchool
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            ContactEmail = contactEmail,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;

    public void UpdateContact(string email) => ContactEmail = email;
}
