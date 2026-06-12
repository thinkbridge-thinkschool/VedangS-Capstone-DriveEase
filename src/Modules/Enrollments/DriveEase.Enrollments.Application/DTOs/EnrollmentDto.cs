namespace DriveEase.Enrollments.Application.DTOs;

public sealed record EnrollmentDto(
    Guid Id,
    Guid StudentId,
    Guid DrivingSchoolId,
    Guid? InstructorId,
    decimal Fee,
    string PaymentStatus,
    string Status,
    DateTime EnrolledAt,
    DateTime? PaymentConfirmedAt);
