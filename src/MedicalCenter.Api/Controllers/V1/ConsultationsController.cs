using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.Consultations;
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
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("horarios")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> CreateScheduleHour([FromBody] ConsultationScheduleHourUpsertRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.CreateScheduleHourAsync(new ConsultationScheduleHourUpsertCommand(request.Hora, request.Orden), cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPatch("horarios/{id:int}")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> UpdateScheduleHour(int id, [FromBody] ConsultationScheduleHourUpsertRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.UpdateScheduleHourAsync(id, new ConsultationScheduleHourUpsertCommand(request.Hora, request.Orden), cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPatch("horarios/{id:int}/estado")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> ToggleScheduleHour(int id, [FromBody] ToggleScheduleHourRequest request, CancellationToken cancellationToken)
    {
        var item = await consultationsService.ToggleScheduleHourAsync(id, request.Activo, cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpGet("horarios/{id:int}/eliminacion-preview")]
    public async Task<IActionResult> PreviewDeleteScheduleHour(int id, CancellationToken cancellationToken)
    {
        var item = await consultationsService.PreviewDeleteScheduleHourAsync(id, cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpDelete("horarios/{id:int}")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> DeleteScheduleHour(int id, CancellationToken cancellationToken)
    {
        var item = await consultationsService.DeleteScheduleHourAsync(id, cancellationToken);
        return item is null ? NoContent() : Ok(item.ToResponse());
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
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("rango")]
    public async Task<IActionResult> GetByRange([FromQuery(Name = "fecha_inicio")] DateOnly fechaInicio, [FromQuery(Name = "fecha_fin")] DateOnly fechaFin, CancellationToken cancellationToken)
    {
        var items = await consultationsService.GetByRangeAsync(fechaInicio, fechaFin, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("{slotId:guid}/asignaciones")]
    public async Task<IActionResult> Assign(Guid slotId, [FromBody] AssignConsultationRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        var item = await consultationsService.AssignAsync(User.GetUserId(), slotId, idempotencyKey, new AssignConsultationCommand(request.PacienteId, request.MedicoId, request.ObservacionesAdmin, request.MedicoUserId), cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/cancelaciones")]
    public async Task<IActionResult> Cancel(Guid slotId, [FromBody] CancelConsultationRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        var item = await consultationsService.CancelAsync(User.GetUserId(), slotId, idempotencyKey, new CancelConsultationCommand(request.Motivo), cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/reprogramaciones")]
    public async Task<IActionResult> Reschedule(Guid slotId, [FromBody] RescheduleConsultationRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        var item = await consultationsService.RescheduleAsync(User.GetUserId(), slotId, idempotencyKey, new RescheduleConsultationCommand(request.TargetSlotId, request.MedicoId, request.MedicoUserId), cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/cierres")]
    public async Task<IActionResult> Close(Guid slotId, [FromBody] CloseConsultationRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        var item = await consultationsService.CloseAsync(User.GetUserId(), slotId, idempotencyKey, new CloseConsultationCommand(request.Estado, request.Titulo, request.Nota, request.DiagnosticoImpresion, request.Indicaciones), cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpGet("pacientes/{pacienteId:guid}/sesiones-completadas")]
    public async Task<IActionResult> GetCompletedSessions(Guid pacienteId, CancellationToken cancellationToken)
    {
        var items = await consultationsService.GetCompletedSessionsAsync(pacienteId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }
}
