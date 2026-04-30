using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class DailyClosingMappings
{
    public static DailyClosingSummaryDto ToSummary(this DailyClosing x) =>
        new(
            x.Id,
            x.Fecha,
            x.Status.ToString().ToLowerInvariant(),
            x.DetallesJson,
            x.CreatedByUserId,
            x.ConfirmedByUserId,
            x.ReopenedByUserId,
            x.MotivoReapertura,
            x.CreatedAt,
            x.UpdatedAt,
            x.ConfirmedAt,
            x.ReopenedAt);
}