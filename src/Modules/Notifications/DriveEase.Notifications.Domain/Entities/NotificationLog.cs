using DriveEase.Shared.Domain;

namespace DriveEase.Notifications.Domain.Entities;

public enum NotificationType { Email, Sms }
public enum NotificationStatus { Sent, Failed }

public sealed class NotificationLog : Entity<Guid>
{
    public Guid RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public string TriggerEvent { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Record(
        Guid recipientId, NotificationType type, string subject, string body, string triggerEvent) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Type = type,
            Subject = subject,
            Body = body,
            TriggerEvent = triggerEvent,
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow
        };
}
