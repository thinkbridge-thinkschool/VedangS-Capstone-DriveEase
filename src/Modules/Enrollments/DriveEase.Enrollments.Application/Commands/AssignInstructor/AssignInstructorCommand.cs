using MediatR;

namespace DriveEase.Enrollments.Application.Commands.AssignInstructor;

public sealed record AssignInstructorCommand(Guid EnrollmentId, Guid InstructorId) : IRequest;
