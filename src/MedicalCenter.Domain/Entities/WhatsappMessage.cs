using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class WhatsappMessage : Entity<Guid>
{
    private WhatsappMessage() { }

    public WhatsappMessage(
        Guid id,
        Guid patientId,
        Guid? slotId,
        Guid? tandaId,
        long? templateId,
        string kind,
        string phoneE164,
        string idempotencyKey,
        string? triggerSource,
        string requestPayload)
    {
        Id = id;
        PatientId = patientId;
        SlotId = slotId;
        TandaId = tandaId;
        TemplateId = templateId;
        Kind = kind;
        PhoneE164 = phoneE164;
        IdempotencyKey = idempotencyKey;
        TriggerSource = triggerSource;
        RequestPayload = requestPayload;
        Status = "queued";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid PatientId { get; private set; }
    public Guid? SlotId { get; private set; }
    public Guid? TandaId { get; private set; }
    public long? TemplateId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Status { get; private set; } = "queued";
    public string? MetaMessageId { get; private set; }
    public string PhoneE164 { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? TriggerSource { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string RequestPayload { get; private set; } = "{}";
    public string? ResponsePayload { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkSent(string? metaMessageId, string responsePayload, DateTimeOffset now)
    {
        Status = "sent";
        MetaMessageId = metaMessageId;
        ResponsePayload = responsePayload;
        DeliveredAt = now;
        FailedAt = null;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = now;
    }

    public void MarkFailed(string errorCode, string errorMessage, string responsePayload, DateTimeOffset now)
    {
        Status = "failed";
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ResponsePayload = responsePayload;
        FailedAt = now;
        UpdatedAt = now;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        Status = "delivered";
        DeliveredAt = now;
        UpdatedAt = now;
    }

    public void MarkRead(DateTimeOffset now)
    {
        Status = "read";
        ReadAt = now;
        UpdatedAt = now;
    }
}
