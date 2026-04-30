using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.DailyClosings;

public interface IDailyClosingsService
{
    Task<DailyClosingPreviewDto> PreviewAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<DailyClosingSummaryDto> ConfirmAsync(Guid actorUserId, DateOnly fecha, string? detallesJson, CancellationToken cancellationToken);
    Task<DailyClosingSummaryDto> GetDetailAsync(DateOnly fecha, Guid? closingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DailyClosingSummaryDto>> GetMonthlyExportAsync(int year, int month, CancellationToken cancellationToken);
    Task<DailyClosingSummaryDto> ReopenAsync(Guid actorUserId, DateOnly fecha, Guid? closingId, string? motivo, CancellationToken cancellationToken);
}
