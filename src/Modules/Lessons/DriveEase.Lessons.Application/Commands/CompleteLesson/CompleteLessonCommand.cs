using MediatR;

namespace DriveEase.Lessons.Application.Commands.CompleteLesson;

public sealed record CompleteLessonCommand(Guid LessonId, string? Notes = null) : IRequest;
