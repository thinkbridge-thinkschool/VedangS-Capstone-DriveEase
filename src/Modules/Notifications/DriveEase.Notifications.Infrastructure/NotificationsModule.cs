using DriveEase.Enrollments.Domain.Events;
using DriveEase.Lessons.Domain.Events;
using DriveEase.Notifications.Application.EventHandlers;
using DriveEase.Notifications.Application.Services;
using DriveEase.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationSender, FakeNotificationSender>();

        // Wire event handlers to the in-memory event bus
        services.AddScoped<IIntegrationEventHandler<EnrollmentConfirmedEvent>, OnEnrollmentConfirmed>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, OnPaymentFailed>();
        services.AddScoped<IIntegrationEventHandler<EnrollmentCancelledEvent>, OnEnrollmentCancelled>();
        services.AddScoped<IIntegrationEventHandler<InstructorAssignedEvent>, OnInstructorAssigned>();
        services.AddScoped<IIntegrationEventHandler<LessonBookedEvent>, OnLessonBooked>();
        services.AddScoped<IIntegrationEventHandler<LessonCompletedEvent>, OnLessonCompleted>();

        return services;
    }
}
