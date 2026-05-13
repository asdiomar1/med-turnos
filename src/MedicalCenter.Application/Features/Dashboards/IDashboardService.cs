using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Dashboards;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetResumenAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardOccupancyCameraDto>> GetOcupacionAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardAgendaRowDto>> GetAgendaAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardUiAlertDto>> GetAlertasAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardWeeklyVolumeItemDto>> GetVolumenSemanalAsync(DateOnly fecha, CancellationToken cancellationToken);
}
