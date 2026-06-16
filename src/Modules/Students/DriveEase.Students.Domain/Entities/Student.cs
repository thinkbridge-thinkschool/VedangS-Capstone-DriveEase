using DriveEase.Shared.Domain;

namespace DriveEase.Students.Domain.Entities;

public sealed class Student : AggregateRoot<Guid>
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;

    private Student() { }

    public static Student Register(string fullName, string email, string phoneNumber, DateOnly dateOfBirth, string passwordHash = "")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        return new Student
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth,
            RegisteredAt = DateTime.UtcNow,
            PasswordHash = passwordHash
        };
    }

    public void UpdateContact(string email, string phoneNumber)
    {
        Email = email;
        PhoneNumber = phoneNumber;
    }
}
