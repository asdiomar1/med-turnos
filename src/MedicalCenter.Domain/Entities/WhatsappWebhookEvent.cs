using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class WhatsappWebhookEvent : Entity<Guid>
{
    private WhatsappWebhookEvent() { }

    public WhatsappWebhookEvent(
        string eventType,
        string metaObject,
        string? entryId,
        string? messageId,
        string payload,
        bool processed = false,
        string? processingError = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        MetaObject = metaObject;
        EntryId = entryId;
        MessageId = messageId;
        Payload = payload;
        Processed = processed;
        ProcessingError = processingError;
        ReceivedAt = DateTimeOffset.UtcNow;
    }

    public string EventType { get; private set; } = string.Empty;
    public string MetaObject { get; private set; } = string.Empty;
    public string? EntryId { get; private set; }
    public string? MessageId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public bool Processed { get; private set; }
    public string? ProcessingError { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }

    public void MarkProcessed()
    {
        Processed = true;
        ProcessingError = null;
    }

    public void MarkFailed(string error)
    {
        Processed = false;
        ProcessingError = error;
    }
}
