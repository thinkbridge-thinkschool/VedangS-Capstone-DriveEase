using DriveEase.Lessons.Domain.Entities;
using DriveEase.Lessons.Domain.Events;
using DriveEase.Lessons.Domain.Repositories;
using DriveEase.Shared.Messaging;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.BookLesson;

public sealed class BookLessonHandler(
    ILessonRepository repository,
    IEventBus eventBus) : IRequestHandler<BookLessonCommand, Guid>
{
    public async Task<Guid> Handle(BookLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = Lesson.Book(
            request.EnrollmentId,
            request.StudentId,
            request.InstructorId,
            request.ScheduledAt,
            request.Duration);

        await repository.AddAsync(lesson, cancellationToken);

        var bookedEvent = (LessonBookedEvent)lesson.DomainEvents.First(e => e is LessonBookedEvent);
        await eventBus.PublishAsync(bookedEvent, cancellationToken);

        lesson.ClearDomainEvents();
        return lesson.Id;
    }
}
