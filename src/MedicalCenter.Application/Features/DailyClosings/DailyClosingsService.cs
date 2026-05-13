using System.Text.Json;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Application.Features.OutOfHoursTurns;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.DailyClosings;

public sealed class DailyClosingsService(
    IUserRepository userRepository,
    IAppointmentRepository appointmentRepository,
    IDailyClosingRepository dailyClosingRepository,
    IAppointmentsService appointmentsService,
    IOutOfHoursTurnsService outOfHoursTurnsService,
    IUnitOfWork unitOfWork) : IDailyClosingsService
{
    public async Task<DailyClosingPreviewDto> PreviewAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var standardTurns = await appointmentsService.GetEnrichedByDateAsync(fecha, cancellationToken);
        var extraTurns = await outOfHoursTurnsService.GetByDateAsync(fecha, cancellationToken);

        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var metrics = BuildMetrics(appointments);
        var alerts = BuildAlerts(metrics, metrics.OcupacionPorcentaje);
        
        var mappedTurnos = MapTurnos(standardTurns, extraTurns);

        return new DailyClosingPreviewDto(
            fecha, 
            metrics.Total, 
            metrics.Libres, 
            metrics.Ocupados, 
            metrics.Apartados, 
            metrics.Cancelados, 
            metrics.OcupacionPorcentaje, 
            metrics.Total > 0, 
            alerts, 
            DateTimeOffset.UtcNow,
            mappedTurnos);
    }

    public Task<DailyClosingSummaryDto> ConfirmAsync(Guid actorUserId, DateOnly fecha, string? detallesJson, CancellationToken cancellationToken) =>
        ConfirmAsync(actorUserId, fecha, null, detallesJson, cancellationToken);

    public async Task<DailyClosingSummaryDto> ConfirmAsync(Guid actorUserId, DateOnly fecha, Guid? closingId, string? detallesJson, CancellationToken cancellationToken)
    {
        await EnsureActorAsync(actorUserId, cancellationToken);
        var closing = await EnsureClosingAsync(fecha, closingId, actorUserId, cancellationToken);
        closing.Confirm(actorUserId, NormalizeJson(detallesJson));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return closing.ToSummary();
    }

    public async Task<DailyClosingSummaryDto> GetDetailAsync(DateOnly fecha, Guid? closingId, CancellationToken cancellationToken)
    {
        var closing = await ResolveClosingAsync(fecha, closingId, cancellationToken);
        if (closing is null)
        {
            return new DailyClosingSummaryDto(
                null,
                fecha,
                "sin_cierre",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        return closing.ToSummary();
    }

    public async Task<IReadOnlyCollection<DailyClosingSummaryDto>> GetMonthlyExportAsync(int year, int month, CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            throw new ValidationException("Mes invalido.");
        }

        var closings = await dailyClosingRepository.GetByMonthAsync(year, month, cancellationToken);
        return closings.Select(c => c.ToSummary()).ToArray();
    }

    public async Task<DailyClosingSummaryDto> ReopenAsync(Guid actorUserId, DateOnly fecha, Guid? closingId, string? motivo, CancellationToken cancellationToken)
    {
        await EnsureActorAsync(actorUserId, cancellationToken);
        var closing = await ResolveClosingAsync(fecha, closingId, cancellationToken);
        if (closing is null)
        {
            throw new NotFoundException("Cierre diario no encontrado");
        }

        closing.Reopen(actorUserId, motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return closing.ToSummary();
    }

    private async Task<DailyClosing> EnsureClosingAsync(DateOnly fecha, Guid? closingId, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (closingId.HasValue)
        {
            return await dailyClosingRepository.GetByIdAsync(closingId.Value, cancellationToken)
                ?? throw new NotFoundException("Cierre diario no encontrado");
        }

        var closing = await dailyClosingRepository.GetByDateAsync(fecha, cancellationToken);
        if (closing is not null)
        {
            return closing;
        }

        closing = new DailyClosing(Guid.NewGuid(), fecha, actorUserId);
        await dailyClosingRepository.AddAsync(closing, cancellationToken);
        return closing;
    }

    private async Task<DailyClosing?> ResolveClosingAsync(DateOnly fecha, Guid? closingId, CancellationToken cancellationToken)
    {
        if (closingId.HasValue)
        {
            return await dailyClosingRepository.GetByIdAsync(closingId.Value, cancellationToken);
        }

        return await dailyClosingRepository.GetByDateAsync(fecha, cancellationToken);
    }

    private async Task EnsureActorAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission("staff.manage"))
        {
            throw new ForbiddenException("Prohibido");
        }
    }

    private static List<DashboardAlertDto> BuildAlerts(Metrics metrics, decimal ocupacionPorcentaje)
    {
        var alerts = new List<DashboardAlertDto>();
        if (metrics.Total == 0)
        {
            alerts.Add(new DashboardAlertDto("day_without_slots", "No hay turnos configurados para la fecha.", "warning", 1));
        }

        if (metrics.Ocupados > 0 && metrics.Total > 0 && ocupacionPorcentaje >= 85m)
        {
            alerts.Add(new DashboardAlertDto("high_occupancy", "La ocupación del día es alta.", "info", 1));
        }

        if (metrics.Apartados > 0)
        {
            alerts.Add(new DashboardAlertDto("apartados_pending", "Hay turnos apartados pendientes.", "warning", metrics.Apartados));
        }

        if (metrics.Cancelados > 0)
        {
            alerts.Add(new DashboardAlertDto("cancelled_turnos", "Hay turnos cancelados en la fecha seleccionada.", "info", metrics.Cancelados));
        }

        return alerts;
    }

    private sealed record Metrics(int Total, int Libres, int Ocupados, int Apartados, int Cancelados)
    {
        public decimal OcupacionPorcentaje => Total <= 0 ? 0 : Math.Round((Ocupados * 100m) / Total, 2);
    }

    private static Metrics BuildMetrics(IEnumerable<Appointment> appointments)
    {
        var items = appointments.ToArray();
        return new Metrics(
            items.Length,
            items.Count(x => x.Status is AppointmentStatus.Libre or AppointmentStatus.Reprogramado),
            items.Count(x => x.Status == AppointmentStatus.Ocupado),
            items.Count(x => x.Status == AppointmentStatus.Apartado),
            items.Count(x => x.Status == AppointmentStatus.Cancelado));
    }

    private static string? NormalizeJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.GetRawText() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<DailyClosingTurnoDto> MapTurnos(
        IEnumerable<TurnoEnrichedSummary> appointments,
        IEnumerable<OutOfHoursTurnSummary> extraTurns)
    {
        var result = new List<DailyClosingTurnoDto>();
        var patientDayCounts = new Dictionary<Guid, int>();

        var orderedAppointments = appointments
            .OrderBy(x => x.Hora)
            .ThenBy(x => x.CamaraId)
            .ThenBy(x => x.Lugar);

        foreach (var appt in orderedAppointments)
        {
            if (appt.PacienteId.HasValue)
            {
                patientDayCounts.TryAdd(appt.PacienteId.Value, 0);
                patientDayCounts[appt.PacienteId.Value]++;
            }

            var referidoTercero = appt.ReferidoTercero ?? false;
            var referenteId = referidoTercero ? appt.ReferenteId : null;
            var referenteNombre = referidoTercero ? appt.Referente?.Nombre : null;
            var referenteTipo = referidoTercero ? NormalizeReferenteTipo(appt.Referente?.Tipo) : null;

            result.Add(new DailyClosingTurnoDto(
                SlotId: appt.Id,
                TurnoFueraHorarioId: null,
                SlotIds: null,
                PacienteId: appt.PacienteId,
                Fecha: appt.Fecha,
                Hora: appt.Hora.ToString("HH:mm"),
                CamaraId: appt.CamaraId,
                CamaraNombre: appt.Camara?.Nombre,
                PacienteNumeroDia: appt.PacienteId.HasValue ? patientDayCounts[appt.PacienteId.Value] : 0,
                NombrePaciente: appt.Paciente?.Nombre,
                SesionNumero: 0, // Simplified: could be calculated if we load cycle progress
                ModalidadCobro: NormalizeModalidadCobro(appt.ModalidadCobro),
                ObraSocialId: appt.ObraSocialId,
                ObraSocialNombre: appt.ObraSocial?.Nombre,
                ObraSocialAbreviatura: appt.ObraSocial?.Abreviatura,
                Importe: 0,
                NumeroAutorizacion: appt.NumeroAutorizacion,
                SesionesAutorizadas: appt.SesionesAutorizadas,
                CicloObraSocialId: appt.CicloObraSocialId,
                EsNuevoIngreso: appt.EsNuevoIngreso ?? false,
                MedicoId: appt.MedicoId,
                MedicoNombre: appt.Medico?.Nombre,
                EsMonoxido: appt.EsMonoxido ?? false,
                EsOxibarica: !(appt.EsMonoxido ?? false),
                Asistio: appt.Estado == "completada",
                ReferidoTercero: referidoTercero,
                ReferenteId: referenteId,
                ReferenteNombre: referenteNombre,
                ReferenteTipo: referenteTipo
            ));
        }

        foreach (var extra in extraTurns.OrderBy(x => x.Hora))
        {
            patientDayCounts.TryAdd(extra.PacienteId, 0);
            patientDayCounts[extra.PacienteId]++;

            result.Add(new DailyClosingTurnoDto(
                SlotId: null,
                TurnoFueraHorarioId: extra.Id,
                SlotIds: null,
                PacienteId: extra.PacienteId,
                Fecha: extra.Fecha,
                Hora: extra.Hora.ToString("HH:mm"),
                CamaraId: null,
                CamaraNombre: "Extra",
                PacienteNumeroDia: patientDayCounts[extra.PacienteId],
                NombrePaciente: extra.Paciente?.Nombre,
                SesionNumero: 0,
                ModalidadCobro: "obra_social",
                ObraSocialId: null,
                ObraSocialNombre: null,
                ObraSocialAbreviatura: null,
                Importe: 0,
                NumeroAutorizacion: null,
                SesionesAutorizadas: null,
                CicloObraSocialId: null,
                EsNuevoIngreso: false,
                MedicoId: extra.MonoxidoMedicoId,
                MedicoNombre: extra.MonoxidoMedico?.Nombre,
                EsMonoxido: extra.EsMonoxido,
                EsOxibarica: !extra.EsMonoxido,
                Asistio: true,
                ReferidoTercero: false,
                ReferenteId: null,
                ReferenteNombre: null,
                ReferenteTipo: null
            ));
        }

        return result;
    }

    private static string NormalizeModalidadCobro(string? modalidadCobro) =>
        string.IsNullOrWhiteSpace(modalidadCobro) ? "particular" : modalidadCobro.Trim();

    private static string? NormalizeReferenteTipo(string? referenteTipo)
    {
        if (string.IsNullOrWhiteSpace(referenteTipo))
        {
            return null;
        }

        return referenteTipo.Trim().ToLowerInvariant() switch
        {
            "agencia" => "agencia",
            "medico" => "medico",
            "otro" => "otro",
            _ => null
        };
    }
}
