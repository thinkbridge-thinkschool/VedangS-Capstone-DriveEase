using DriveEase.Lessons.Domain.Events;
using DriveEase.Lessons.Domain.Repositories;
using DriveEase.Shared.Messaging;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.CompleteLesson;

public sealed class CompleteLessonHandler(
    ILessonRepository repository,
    IEventBus eventBus) : IRequestHandler<CompleteLessonCommand>
{
    public async Task Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await repository.GetByIdAsync(request.LessonId, cancellationToken)
            ?? throw new InvalidOperationException($"Lesson {request.LessonId} not found.");

        lesson.Complete(request.Notes);
        await repository.UpdateAsync(lesson, cancellationToken);

        var completedEvent = (LessonCompletedEvent)lesson.DomainEvents.First(e => e is LessonCompletedEvent);
        await eventBus.PublishAsync(completedEvent, cancellationToken);

        lesson.ClearDomainEvents();
    }
}
