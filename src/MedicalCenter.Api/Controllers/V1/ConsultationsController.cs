using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.Consultations;
using MedicalCenter.Contracts.Common;
using MedicalCenter.Contracts.Consultations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/consultas")]
[Authorize(Policy = "ConsultasRead")]
public sealed class ConsultationsController(IConsultationsService consultationsService) : ControllerBase
{
    [HttpGet("horarios")]
    public async Task<IActionResult> GetScheduleHours(CancellationToken cancellationToken)
    {
        var items = await consultationsService.GetScheduleHoursAsync(cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpPost("horarios")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> CreateScheduleHour([FromBody] ConsultationScheduleHourUpsertRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.CreateScheduleHourAsync(new ConsultationScheduleHourUpsertCommand(request.Hora, request.Orden), cancellationToken);
        return Ok(Map(item));
    }

    [HttpPatch("horarios/{id:int}")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> UpdateScheduleHour(int id, [FromBody] ConsultationScheduleHourUpsertRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.UpdateScheduleHourAsync(id, new ConsultationScheduleHourUpsertCommand(request.Hora, request.Orden), cancellationToken);
        return Ok(Map(item));
    }

    [HttpPatch("horarios/{id:int}/estado")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> ToggleScheduleHour(int id, [FromBody] ToggleScheduleHourRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.ToggleScheduleHourAsync(id, request.Activo, cancellationToken);
        return Ok(Map(item));
    }

    [HttpGet("horarios/{id:int}/eliminacion-preview")]
    public async Task<IActionResult> PreviewDeleteScheduleHour(int id, CancellationToken cancellationToken)
    {
        var item = await consultationsService.PreviewDeleteScheduleHourAsync(id, cancellationToken);
        return Ok(new ConsultationScheduleHourDeletionPreviewResponse
        {
            Id = item.Id,
            Hora = item.Hora,
            CanDelete = item.CanDelete,
            FutureSlotsCount = item.FutureSlotsCount
        });
    }

    [HttpDelete("horarios/{id:int}")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> DeleteScheduleHour(int id, CancellationToken cancellationToken)
    {
        var item = await consultationsService.DeleteScheduleHourAsync(id, cancellationToken);
        return item is null ? NoContent() : Ok(Map(item));
    }

    [HttpPost("generar")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> Generate([FromBody] GenerateConsultationsRequest request, CancellationToken cancellationToken)
    {
        var total = await consultationsService.GenerateAsync(request.Fecha, cancellationToken);
        return Ok(new { total });
    }

    [HttpPost("reparar")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> Repair([FromBody] RepairConsultationsRangeRequest request, CancellationToken cancellationToken)
    {
        var total = await consultationsService.RepairRangeAsync(request.FechaInicio, request.FechaFin, cancellationToken);
        return Ok(new { total });
    }

    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var items = await consultationsService.GetByDateAsync(fecha, cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpGet("rango")]
    public async Task<IActionResult> GetByRange([FromQuery(Name = "fecha_inicio")] DateOnly fechaInicio, [FromQuery(Name = "fecha_fin")] DateOnly fechaFin, CancellationToken cancellationToken)
    {
        var items = await consultationsService.GetByRangeAsync(fechaInicio, fechaFin, cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpPost("{slotId:guid}/asignaciones")]
    public async Task<IActionResult> Assign(Guid slotId, [FromBody] AssignConsultationRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.AssignAsync(User.GetUserId(), slotId, GetRequiredIdempotencyKey(), new AssignConsultationCommand(request.PacienteId, request.MedicoId, request.ObservacionesAdmin), cancellationToken);
        return Ok(Map(item));
    }

    [HttpPost("{slotId:guid}/cancelaciones")]
    public async Task<IActionResult> Cancel(Guid slotId, [FromBody] CancelConsultationRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.CancelAsync(User.GetUserId(), slotId, GetRequiredIdempotencyKey(), new CancelConsultationCommand(request.Motivo), cancellationToken);
        return Ok(Map(item));
    }

    [HttpPost("{slotId:guid}/reprogramaciones")]
    public async Task<IActionResult> Reschedule(Guid slotId, [FromBody] RescheduleConsultationRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.RescheduleAsync(User.GetUserId(), slotId, GetRequiredIdempotencyKey(), new RescheduleConsultationCommand(request.TargetSlotId, request.MedicoId), cancellationToken);
        return Ok(Map(item));
    }

    [HttpPost("{slotId:guid}/cierres")]
    public async Task<IActionResult> Close(Guid slotId, [FromBody] CloseConsultationRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.CloseAsync(User.GetUserId(), slotId, GetRequiredIdempotencyKey(), new CloseConsultationCommand(request.Estado, request.Titulo, request.Nota, request.DiagnosticoImpresion, request.Indicaciones), cancellationToken);
        return Ok(Map(item));
    }

    [HttpGet("pacientes/{pacienteId:guid}/sesiones-completadas")]
    public async Task<IActionResult> GetCompletedSessions(Guid pacienteId, CancellationToken cancellationToken)
    {
        var items = await consultationsService.GetCompletedSessionsAsync(pacienteId, cancellationToken);
        return Ok(items.Select(Map));
    }

    private string GetRequiredIdempotencyKey() =>
        Request.Headers.TryGetValue("Idempotency-Key", out var values) ? values.ToString() : string.Empty;

    private static ConsultationScheduleHourResponse Map(ConsultationScheduleHourSummary x) => new()
    {
        Id = x.Id,
        Hora = x.Hora,
        Activo = x.Activo,
        Orden = x.Orden,
        CreatedAt = x.CreatedAt
    };

    private static ConsultationScheduleHourDeletionPreviewResponse Map(ConsultationScheduleHourDeletionPreviewSummary x) => new()
    {
        Id = x.Id,
        Hora = x.Hora,
        CanDelete = x.CanDelete,
        FutureSlotsCount = x.FutureSlotsCount
    };

    private static ConsultationSlotResponse Map(ConsultationSlotSummary x) => new()
    {
        Id = x.Id,
        Fecha = x.Fecha,
        Hora = x.Hora,
        Estado = x.Estado,
        PacienteId = x.PacienteId,
        MedicoId = x.MedicoId,
        MotivoCancelacion = x.MotivoCancelacion,
        ObservacionesAdmin = x.ObservacionesAdmin,
        ConfirmadoPor = x.ConfirmadoPor,
        ConfirmadoAt = x.ConfirmadoAt,
        CerradoPor = x.CerradoPor,
        CerradoAt = x.CerradoAt,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        Paciente = x.Paciente is null ? null : new GuidLookupResponse { Id = x.Paciente.Id, Nombre = x.Paciente.Nombre, DocumentoIdentidad = x.Paciente.DocumentoIdentidad, Email = x.Paciente.Email, Activo = x.Paciente.Activo },
        Medico = x.Medico is null ? null : new IntLookupResponse { Id = x.Medico.Id, Nombre = x.Medico.Nombre, Extra = x.Medico.Extra, Activo = x.Medico.Activo },
        ConfirmadoPorPerfil = x.ConfirmadoPorPerfil is null ? null : new GuidLookupResponse { Id = x.ConfirmadoPorPerfil.Id, Nombre = x.ConfirmadoPorPerfil.Nombre, DocumentoIdentidad = x.ConfirmadoPorPerfil.DocumentoIdentidad, Email = x.ConfirmadoPorPerfil.Email, Activo = x.ConfirmadoPorPerfil.Activo },
        CerradoPorPerfil = x.CerradoPorPerfil is null ? null : new GuidLookupResponse { Id = x.CerradoPorPerfil.Id, Nombre = x.CerradoPorPerfil.Nombre, DocumentoIdentidad = x.CerradoPorPerfil.DocumentoIdentidad, Email = x.CerradoPorPerfil.Email, Activo = x.CerradoPorPerfil.Activo }
    };

    private static ConsultationSessionResponse Map(ConsultationSessionSummary x) => new()
    {
        Id = x.Id,
        PacienteId = x.PacienteId,
        SlotId = x.SlotId,
        Fecha = x.Fecha,
        Hora = x.Hora,
        CamaraId = x.CamaraId,
        CreatedAt = x.CreatedAt,
        ModalidadCobro = x.ModalidadCobro,
        ObraSocialId = x.ObraSocialId,
        CierreId = x.CierreId,
        NumeroAutorizacion = x.NumeroAutorizacion,
        SesionesAutorizadas = x.SesionesAutorizadas,
        CicloObraSocialId = x.CicloObraSocialId
    };
}
