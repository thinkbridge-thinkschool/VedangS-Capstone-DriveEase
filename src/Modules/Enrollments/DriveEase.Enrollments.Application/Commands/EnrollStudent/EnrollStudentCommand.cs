using MediatR;

namespace DriveEase.Enrollments.Application.Commands.EnrollStudent;

public sealed record EnrollStudentCommand(
    Guid StudentId,
    Guid DrivingSchoolId,
    decimal Fee) : IRequest<Guid>;
