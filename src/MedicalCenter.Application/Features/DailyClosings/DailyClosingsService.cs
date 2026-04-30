using System.Text.Json;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.DailyClosings;

public sealed class DailyClosingsService(
    IUserRepository userRepository,
    IAppointmentRepository appointmentRepository,
    IDailyClosingRepository dailyClosingRepository,
    IUnitOfWork unitOfWork) : IDailyClosingsService
{
    public async Task<DailyClosingPreviewDto> PreviewAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var metrics = BuildMetrics(appointments);
        var alerts = BuildAlerts(metrics, metrics.OcupacionPorcentaje);
        return new DailyClosingPreviewDto(fecha, metrics.Total, metrics.Libres, metrics.Ocupados, metrics.Apartados, metrics.Cancelados, metrics.OcupacionPorcentaje, metrics.Total > 0, alerts, DateTimeOffset.UtcNow);
    }

    public async Task<DailyClosingSummaryDto> ConfirmAsync(Guid actorUserId, DateOnly fecha, string? detallesJson, CancellationToken cancellationToken)
    {
        await EnsureActorAsync(actorUserId, cancellationToken);
        var closing = await EnsureClosingAsync(fecha, actorUserId, cancellationToken);
        closing.Confirm(actorUserId, NormalizeJson(detallesJson));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(closing);
    }

    public async Task<DailyClosingSummaryDto> GetDetailAsync(DateOnly fecha, Guid? closingId, CancellationToken cancellationToken)
    {
        var closing = await ResolveClosingAsync(fecha, closingId, cancellationToken);
        if (closing is null)
        {
            return new DailyClosingSummaryDto(
                Guid.Empty,
                fecha,
                DailyClosingStatus.Pending.ToString().ToLowerInvariant(),
                null,
                Guid.Empty,
                null,
                null,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null);
        }

        return Map(closing);
    }

    public async Task<IReadOnlyCollection<DailyClosingSummaryDto>> GetMonthlyExportAsync(int year, int month, CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            throw new ValidationException("Mes invalido.");
        }

        var closings = await dailyClosingRepository.GetByMonthAsync(year, month, cancellationToken);
        return closings.Select(Map).ToArray();
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
        return Map(closing);
    }

    private async Task<DailyClosing> EnsureClosingAsync(DateOnly fecha, Guid actorUserId, CancellationToken cancellationToken)
    {
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

    private static IReadOnlyCollection<DashboardAlertDto> BuildAlerts(Metrics metrics, decimal ocupacionPorcentaje)
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
            items.Count(x => x.Status == AppointmentStatus.Libre),
            items.Count(x => x.Status == AppointmentStatus.Ocupado),
            items.Count(x => x.Status == AppointmentStatus.Apartado),
            items.Count(x => x.Status == AppointmentStatus.Cancelado));
    }

    private static DailyClosingSummaryDto Map(DailyClosing closing) =>
        new(closing.Id, closing.Fecha, closing.Status.ToString().ToLowerInvariant(), closing.DetallesJson, closing.CreatedByUserId, closing.ConfirmedByUserId, closing.ReopenedByUserId, closing.MotivoReapertura, closing.CreatedAt, closing.UpdatedAt, closing.ConfirmedAt, closing.ReopenedAt);

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
}
