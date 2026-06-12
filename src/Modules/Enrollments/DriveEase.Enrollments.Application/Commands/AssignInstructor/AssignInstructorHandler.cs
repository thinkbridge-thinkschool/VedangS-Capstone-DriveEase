using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Shared.Messaging;
using DriveEase.Enrollments.Domain.Events;
using MediatR;

namespace DriveEase.Enrollments.Application.Commands.AssignInstructor;

public sealed class AssignInstructorHandler(
    IEnrollmentRepository repository,
    IEventBus eventBus) : IRequestHandler<AssignInstructorCommand>
{
    public async Task Handle(AssignInstructorCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Enrollment {request.EnrollmentId} not found.");

        enrollment.AssignInstructor(request.InstructorId);
        await repository.UpdateAsync(enrollment, cancellationToken);

        var assignedEvent = (InstructorAssignedEvent)enrollment.DomainEvents
            .First(e => e is InstructorAssignedEvent);
        await eventBus.PublishAsync(assignedEvent, cancellationToken);

        enrollment.ClearDomainEvents();
    }
}
