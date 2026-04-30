using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Domain.Entities;

public sealed class OperationRequest : Entity<Guid>
{
    private OperationRequest() { }

    public OperationRequest(Guid id, string operation, string key, string requestHash, DateTimeOffset createdAt)
    {
        Id = id;
        Operation = operation;
        Key = key;
        RequestHash = requestHash;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Status = OperationRequestStatus.Pending;
    }

    public string Operation { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public OperationRequestStatus Status { get; private set; }
    public string? ResponsePayload { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool HasSamePayload(string requestHash) =>
        RequestHash.Equals(requestHash, StringComparison.OrdinalIgnoreCase);

    public void Reopen(string requestHash, DateTimeOffset utcNow)
    {
        RequestHash = requestHash;
        Status = OperationRequestStatus.Pending;
        ResponsePayload = null;
        CompletedAt = null;
        UpdatedAt = utcNow;
    }

    public void MarkCompleted(string responsePayload, DateTimeOffset utcNow)
    {
        Status = OperationRequestStatus.Completed;
        ResponsePayload = responsePayload;
        CompletedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void MarkFailed(DateTimeOffset utcNow)
    {
        Status = OperationRequestStatus.Failed;
        UpdatedAt = utcNow;
    }
}
