using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.Dashboards;

public sealed class DashboardService(
    IAppointmentRepository appointmentRepository,
    ICameraRepository cameraRepository,
    IPatientRepository patientRepository) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetResumenAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var metrics = BuildMetrics(appointments);
        return new DashboardSummaryDto(metrics.Ocupados, metrics.Apartados);
    }

    public async Task<IReadOnlyCollection<DashboardOccupancyCameraDto>> GetOcupacionAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var cameras = await cameraRepository.GetAsync(cancellationToken);
        var ocupadosPorCamara = appointments
            .Where(x => x.CameraId.HasValue && x.Status == AppointmentStatus.Ocupado)
            .GroupBy(x => x.CameraId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        return cameras
            .OrderBy(x => x.Id)
            .Select(camera =>
            {
                var ocupados = ocupadosPorCamara.GetValueOrDefault(camera.Id, 0);
                var porcentaje = camera.Capacidad <= 0 ? 0 : (int)Math.Round((ocupados * 100m) / camera.Capacidad, MidpointRounding.AwayFromZero);
                return new DashboardOccupancyCameraDto(
                    camera.Id,
                    camera.Nombre,
                    camera.Capacidad,
                    ocupados,
                    porcentaje);
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DashboardAgendaRowDto>> GetAgendaAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var cameras = await cameraRepository.GetAsync(cancellationToken);
        var patientIds = appointments
            .Where(x => x.PatientId.HasValue)
            .Select(x => x.PatientId!.Value)
            .Distinct()
            .ToArray();
        var patients = patientIds.Length == 0
            ? []
            : await patientRepository.GetByIdsAsync(patientIds, cancellationToken);
        var cameraLookup = cameras.ToDictionary(x => x.Id, x => x.Nombre);
        var patientLookup = patients.ToDictionary(x => x.Id, x => x.Nombre);

        return appointments
            .Where(x => x.CameraId.HasValue && x.Status is AppointmentStatus.Ocupado or AppointmentStatus.Apartado)
            .Select(appointment =>
            {
                cameraLookup.TryGetValue(appointment.CameraId!.Value, out var cameraName);
                var patientName = appointment.PatientId.HasValue && patientLookup.TryGetValue(appointment.PatientId.Value, out var value)
                    ? value
                    : string.Empty;

                return new DashboardAgendaRowDto(
                    appointment.Hora,
                    appointment.Lugar,
                    appointment.CameraId.Value,
                    cameraName ?? string.Empty,
                    patientName,
                    appointment.ModalidadCobro,
                    appointment.EsNuevoIngreso,
                    appointment.EsBloqueCompleto,
                    MapAgendaEstado(appointment.Status));
            })
            .OrderBy(x => x.Hora)
            .ThenBy(x => x.CamaraId)
            .ThenBy(x => x.Lugar)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DashboardUiAlertDto>> GetAlertasAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var metrics = BuildMetrics(appointments);
        var alerts = new List<DashboardUiAlertDto>();

        if (metrics.Total == 0)
        {
            alerts.Add(new DashboardUiAlertDto(
                "dia_sin_turnos",
                "Día sin turnos",
                "No hay turnos configurados para la fecha seleccionada.",
                "agenda"));
        }

        if (metrics.OcupacionPorcentaje >= 85m && metrics.Total > 0)
        {
            alerts.Add(new DashboardUiAlertDto(
                "ocupacion_alta",
                "Ocupación alta",
                "La ocupación del día superó el umbral esperado.",
                "ocupacion"));
        }

        if (metrics.Apartados > 0)
        {
            alerts.Add(new DashboardUiAlertDto(
                "apartados_pendientes",
                "Apartados pendientes",
                "Hay turnos apartados pendientes de confirmación o liberación.",
                "agenda"));
        }

        if (metrics.Cancelados > 0)
        {
            alerts.Add(new DashboardUiAlertDto(
                "turnos_cancelados",
                "Turnos cancelados",
                "Hay turnos cancelados en la fecha seleccionada.",
                "agenda"));
        }

        return alerts;
    }

    public async Task<IReadOnlyCollection<DashboardWeeklyVolumeItemDto>> GetVolumenSemanalAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var fromDate = fecha.AddDays(-6);
        var appointments = await appointmentRepository.GetByRangeAsync(fromDate, fecha, null, null, cancellationToken);
        var ocupadosPorDia = appointments
            .Where(x => x.Status == AppointmentStatus.Ocupado)
            .GroupBy(x => x.Fecha)
            .ToDictionary(group => group.Key, group => group.Count());

        return Enumerable.Range(0, 7)
            .Select(offset => fromDate.AddDays(offset))
            .Select(day => new DashboardWeeklyVolumeItemDto(day, ocupadosPorDia.GetValueOrDefault(day, 0)))
            .OrderBy(x => x.Fecha)
            .ToArray();
    }

    private static Metrics BuildMetrics(IEnumerable<Domain.Entities.Appointment> appointments)
    {
        var items = appointments.ToArray();
        return new Metrics(
            items.Length,
            items.Count(x => x.Status is AppointmentStatus.Libre or AppointmentStatus.Reprogramado),
            items.Count(x => x.Status == AppointmentStatus.Ocupado),
            items.Count(x => x.Status == AppointmentStatus.Apartado),
            items.Count(x => x.Status == AppointmentStatus.Cancelado));
    }

    private sealed record Metrics(int Total, int Libres, int Ocupados, int Apartados, int Cancelados)
    {
        public decimal OcupacionPorcentaje => Total <= 0 ? 0 : Math.Round((Ocupados * 100m) / Total, 2);
    }

    private static string MapAgendaEstado(AppointmentStatus status) =>
        status switch
        {
            AppointmentStatus.Ocupado => "asignado",
            AppointmentStatus.Apartado => "apartado",
            AppointmentStatus.Libre => "libre",
            AppointmentStatus.Cancelado => "cancelado",
            AppointmentStatus.Reprogramado => "reprogramado",
            _ => "desconocido"
        };
}
