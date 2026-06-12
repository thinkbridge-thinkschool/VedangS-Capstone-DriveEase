using DriveEase.Notifications.Application.Services;
using Microsoft.Extensions.Logging;

namespace DriveEase.Notifications.Infrastructure;

public sealed class FakeNotificationSender(ILogger<FakeNotificationSender> logger) : INotificationSender
{
    public Task SendEmailAsync(Guid recipientId, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[EMAIL] To: {RecipientId} | Subject: {Subject} | Body: {Body}",
            recipientId, subject, body);
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(Guid recipientId, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SMS] To: {RecipientId} | Message: {Message}", recipientId, message);
        return Task.CompletedTask;
    }
}
