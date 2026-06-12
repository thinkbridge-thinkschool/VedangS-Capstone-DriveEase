using DriveEase.Enrollments.Application.DTOs;
using DriveEase.Enrollments.Domain.Repositories;
using MediatR;

namespace DriveEase.Enrollments.Application.Queries.GetEnrollment;

public sealed class GetEnrollmentHandler(IEnrollmentRepository repository)
    : IRequestHandler<GetEnrollmentQuery, EnrollmentDto?>
{
    public async Task<EnrollmentDto?> Handle(GetEnrollmentQuery request, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment is null) return null;

        return new EnrollmentDto(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.DrivingSchoolId,
            enrollment.InstructorId,
            enrollment.Fee,
            enrollment.PaymentStatus.ToString(),
            enrollment.Status.ToString(),
            enrollment.EnrolledAt,
            enrollment.PaymentConfirmedAt);
    }
}
