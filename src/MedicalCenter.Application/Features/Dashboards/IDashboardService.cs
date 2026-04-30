using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Dashboards;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetResumenAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<DashboardOccupancyDto> GetOcupacionAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardAgendaBucketDto>> GetAgendaAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardAlertDto>> GetAlertasAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardWeeklyVolumeItemDto>> GetVolumenSemanalAsync(DateOnly fecha, CancellationToken cancellationToken);
}
