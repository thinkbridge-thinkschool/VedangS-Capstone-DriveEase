using DriveEase.Enrollments.Domain.Aggregates;
using DriveEase.Enrollments.Domain.Repositories;
using MediatR;

namespace DriveEase.Enrollments.Application.Commands.EnrollStudent;

public sealed class EnrollStudentHandler(IEnrollmentRepository repository) : IRequestHandler<EnrollStudentCommand, Guid>
{
    public async Task<Guid> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetActiveByStudentIdAsync(request.StudentId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("Student already has an active enrollment.");

        var enrollment = Enrollment.Create(request.StudentId, request.DrivingSchoolId, request.Fee);

        await repository.AddAsync(enrollment, cancellationToken);

        return enrollment.Id;
    }
}
