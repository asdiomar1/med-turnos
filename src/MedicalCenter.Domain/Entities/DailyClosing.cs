using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Domain.Entities;

public sealed class DailyClosing : Entity<Guid>
{
    private DailyClosing() { }

    public DailyClosing(Guid id, DateOnly fecha, Guid createdByUserId, string? detallesJson = null)
    {
        Id = id;
        Fecha = fecha;
        CreatedByUserId = createdByUserId;
        Status = DailyClosingStatus.Pending;
        DetallesJson = detallesJson;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public DateOnly Fecha { get; private set; }
    public DailyClosingStatus Status { get; private set; }
    public string? DetallesJson { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
    public Guid? ReopenedByUserId { get; private set; }
    public string? MotivoReapertura { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? ReopenedAt { get; private set; }

    public void Confirm(Guid confirmedByUserId, string? detallesJson)
    {
        Status = DailyClosingStatus.Confirmed;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedAt = DateTimeOffset.UtcNow;
        DetallesJson = detallesJson ?? DetallesJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reopen(Guid reopenedByUserId, string? motivo = null)
    {
        Status = DailyClosingStatus.Reopened;
        ReopenedByUserId = reopenedByUserId;
        ReopenedAt = DateTimeOffset.UtcNow;
        MotivoReapertura = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
