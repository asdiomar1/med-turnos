using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed record WhatsappDispatchQueueItemCreateParams(
    Guid Id,
    Guid PatientId,
    Guid? SlotId,
    Guid? TandaId,
    string Kind,
    string TemplateKey,
    string IdempotencyKey,
    string TriggerSource,
    string Payload);

public sealed class WhatsappDispatchQueueItem : Entity<Guid>
{
    private WhatsappDispatchQueueItem() { }

    public WhatsappDispatchQueueItem(WhatsappDispatchQueueItemCreateParams p)
    {
        Id = p.Id;
        PatientId = p.PatientId;
        SlotId = p.SlotId;
        TandaId = p.TandaId;
        Kind = p.Kind;
        TemplateKey = p.TemplateKey;
        IdempotencyKey = p.IdempotencyKey;
        TriggerSource = p.TriggerSource;
        Payload = p.Payload;
        Status = "pending";
        Attempts = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid PatientId { get; private set; }
    public Guid? SlotId { get; private set; }
    public Guid? TandaId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string TemplateKey { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string TriggerSource { get; private set; } = "system";
    public string Payload { get; private set; } = "{}";
    public string Status { get; private set; } = "pending";
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkProcessing(DateTimeOffset now)
    {
        Status = "processing";
        Attempts += 1;
        LockedAt = now;
        UpdatedAt = now;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        Status = "processed";
        ProcessedAt = now;
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkSkipped(string? reason, DateTimeOffset now)
    {
        Status = "skipped";
        LastError = reason;
        ProcessedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = "failed";
        LastError = error;
        UpdatedAt = now;
    }
}
