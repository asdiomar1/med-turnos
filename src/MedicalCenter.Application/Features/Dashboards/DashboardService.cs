using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.Dashboards;

public sealed class DashboardService(
    IAppointmentRepository appointmentRepository,
    ICameraRepository cameraRepository) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetResumenAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var metrics = BuildMetrics(appointments);
        return new DashboardSummaryDto(fecha, metrics.Total, metrics.Libres, metrics.Ocupados, metrics.Apartados, metrics.Cancelados, metrics.OcupacionPorcentaje, DateTimeOffset.UtcNow);
    }

    public async Task<DashboardOccupancyDto> GetOcupacionAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var cameras = await cameraRepository.GetAsync(cancellationToken);
        var cameraLookup = cameras.ToDictionary(x => x.Id, x => x.Nombre);

        var grouped = appointments
            .GroupBy(x => x.CameraId)
            .Select(group =>
            {
                var metrics = BuildMetrics(group);
                cameraLookup.TryGetValue(group.Key ?? 0, out var cameraName);
                return new DashboardOccupancyCameraDto(group.Key, cameraName, metrics.Total, metrics.Libres, metrics.Ocupados, metrics.Apartados, metrics.Cancelados);
            })
            .OrderBy(x => x.CameraId)
            .ToArray();

        var total = BuildMetrics(appointments);
        return new DashboardOccupancyDto(fecha, total.Total, total.Libres, total.Ocupados, total.Apartados, total.Cancelados, total.OcupacionPorcentaje, grouped);
    }

    public async Task<IReadOnlyCollection<DashboardAgendaBucketDto>> GetAgendaAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var cameras = await cameraRepository.GetAsync(cancellationToken);
        var cameraLookup = cameras.ToDictionary(x => x.Id, x => x.Nombre);

        return appointments
            .GroupBy(x => new { x.Fecha, x.Hora, x.CameraId })
            .Select(group =>
            {
                cameraLookup.TryGetValue(group.Key.CameraId ?? 0, out var cameraName);
                var metrics = BuildMetrics(group);
                return new DashboardAgendaBucketDto(group.Key.Fecha, group.Key.Hora, group.Key.CameraId, cameraName, metrics.Total, metrics.Libres, metrics.Ocupados, metrics.Apartados, metrics.Cancelados);
            })
            .OrderBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DashboardAlertDto>> GetAlertasAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var metrics = BuildMetrics(appointments);
        var alerts = new List<DashboardAlertDto>();

        if (metrics.Total == 0)
        {
            alerts.Add(new DashboardAlertDto("day_without_slots", "No hay turnos configurados para la fecha.", "warning", 1));
        }

        if (metrics.OcupacionPorcentaje >= 85m && metrics.Total > 0)
        {
            alerts.Add(new DashboardAlertDto("high_occupancy", "La ocupación del día es alta.", "info", 1));
        }

        if (metrics.Apartados > 0)
        {
            alerts.Add(new DashboardAlertDto("apartados_pending", "Hay turnos apartados pendientes de confirmación o liberación.", "warning", metrics.Apartados));
        }

        if (metrics.Cancelados > 0)
        {
            alerts.Add(new DashboardAlertDto("cancelled_turnos", "Hay turnos cancelados en la fecha seleccionada.", "info", metrics.Cancelados));
        }

        return alerts;
    }

    public async Task<IReadOnlyCollection<DashboardWeeklyVolumeItemDto>> GetVolumenSemanalAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var fromDate = fecha.AddDays(-6);
        var appointments = await appointmentRepository.GetByRangeAsync(fromDate, fecha, cancellationToken);

        return appointments
            .GroupBy(x => x.Fecha)
            .Select(group =>
            {
                var metrics = BuildMetrics(group);
                return new DashboardWeeklyVolumeItemDto(group.Key, metrics.Total, metrics.Libres, metrics.Ocupados, metrics.Apartados, metrics.Cancelados);
            })
            .OrderBy(x => x.Fecha)
            .ToArray();
    }

    private static Metrics BuildMetrics(IEnumerable<Domain.Entities.Appointment> appointments)
    {
        var items = appointments.ToArray();
        return new Metrics(
            items.Length,
            items.Count(x => x.Status == AppointmentStatus.Libre),
            items.Count(x => x.Status == AppointmentStatus.Ocupado),
            items.Count(x => x.Status == AppointmentStatus.Apartado),
            items.Count(x => x.Status == AppointmentStatus.Cancelado));
    }

    private sealed record Metrics(int Total, int Libres, int Ocupados, int Apartados, int Cancelados)
    {
        public decimal OcupacionPorcentaje => Total <= 0 ? 0 : Math.Round((Ocupados * 100m) / Total, 2);
    }
}
