namespace DriveEase.Notifications.Application.Services;

public interface INotificationSender
{
    Task SendEmailAsync(Guid recipientId, string subject, string body, CancellationToken cancellationToken = default);
    Task SendSmsAsync(Guid recipientId, string message, CancellationToken cancellationToken = default);
}
