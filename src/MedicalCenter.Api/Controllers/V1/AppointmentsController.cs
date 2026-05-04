using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Contracts.Appointments;
using MedicalCenter.Contracts.Consultations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/turnos")]
[Authorize]
public sealed class AppointmentsController(
    IAppointmentsService appointmentsService,
    ICameraRepository cameraRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetByDateAsync(fecha, cancellationToken);
        var cameras = (await cameraRepository.GetAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => new AppointmentCameraResponse
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Capacidad = x.Capacidad,
                Activa = x.Activa,
            });

        return Ok(items.Select(x => x.ToResponse(cameras)));
    }

    [HttpGet("disponibles-portal")]
    public async Task<IActionResult> GetDisponiblesPortal([FromQuery] DateOnly fecha, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetDisponiblesPortalByDateAsync(User.GetUserId(), fecha, cancellationToken);
        var cameras = (await cameraRepository.GetAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => new AppointmentCameraResponse
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Capacidad = x.Capacidad,
                Activa = x.Activa,
            });

        return Ok(items.Select(x => x.ToResponse(cameras)));
    }

    [HttpGet("rango")]
    public async Task<IActionResult> GetByRange(
        [FromQuery(Name = "fecha_inicio")] DateOnly fechaInicio,
        [FromQuery(Name = "fecha_fin")] DateOnly fechaFin,
        CancellationToken cancellationToken,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 5000)
    {
        if (limit < 1) limit = 1;
        if (limit > 50000) limit = 50000;
        if (offset < 0) offset = 0;
        var result = await appointmentsService.GetEnrichedByRangeAsync(fechaInicio, fechaFin, offset, limit, cancellationToken);
        return Ok(new
        {
            items = result.Items.Select(x => x.ToTurnoEnrichedResponse()).ToArray(),
            total = result.Total,
        });
    }

    [HttpGet("fecha")]
    public async Task<IActionResult> GetByFecha([FromQuery] DateOnly fecha, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetEnrichedByDateAsync(fecha, cancellationToken);
        return Ok(items.Select(x => x.ToTurnoEnrichedResponse()).ToArray());
    }

    [HttpPost("generar")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> Generate([FromBody] GenerateAppointmentsRequest request, CancellationToken cancellationToken)
    {
        var total = await appointmentsService.GenerateAsync(request.Fecha, cancellationToken);
        return Ok(new { total });
    }

    [HttpPost("reparar")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> Repair([FromBody] RepairConsultationsRangeRequest request, CancellationToken cancellationToken)
    {
        var total = await appointmentsService.RepairRangeAsync(request.FechaInicio, request.FechaFin, cancellationToken);
        return Ok(new { total });
    }

    [HttpGet("pacientes/{pacienteId:guid}/activos")]
    public async Task<IActionResult> GetActivosByPaciente(Guid pacienteId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetActivosByPacienteAsync(pacienteId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("{slotId:guid}/asignaciones")]
    public async Task<IActionResult> Assign(Guid slotId, [FromBody] AssignAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.AssignAsync(
            User.GetUserId(),
            slotId,
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.AssignAppointmentCommand(
                request.PacienteId,
                request.EsTanda,
                request.TandaId,
                request.Accion,
                request.ReferidoTercero,
                request.ReferenteId,
                request.ModalidadCobro,
                request.ObraSocialId,
                request.NumeroAutorizacion,
                request.SesionesAutorizadas,
                request.CicloObraSocialId,
                request.IniciarNuevoCicloObraSocial,
                request.ConvenioCorroborado,
                request.MedicoId,
                request.MedicoUserId,
                request.EsNuevoIngreso,
                request.EsMonoxido,
                request.MonoxidoOrdenMedica,
                request.MonoxidoResumenClinico),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/cancelaciones")]
    public async Task<IActionResult> Cancel(Guid slotId, [FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.CancelAsync(User.GetUserId(), slotId, GetRequiredIdempotencyKey(), request.Motivo, cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/reprogramaciones")]
    public async Task<IActionResult> Reschedule(Guid slotId, [FromBody] RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.RescheduleAsync(
            User.GetUserId(),
            slotId,
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.RescheduleAppointmentCommand(request.TargetSlotId, request.Scope),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/apartados")]
    public async Task<IActionResult> Hold(Guid slotId, [FromBody] HoldAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.HoldAsync(
            User.GetUserId(),
            slotId,
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.HoldAppointmentCommand(
                request.PacienteId,
                request.EsMonoxido,
                request.ReferidoTercero,
                request.ReferenteId,
                request.ModalidadCobro,
                request.ObraSocialId,
                request.NumeroAutorizacion,
                request.SesionesAutorizadas,
                request.CicloObraSocialId,
                request.IniciarNuevoCicloObraSocial,
                request.ConvenioCorroborado,
                request.MedicoId,
                request.MedicoUserId,
                request.EsNuevoIngreso,
                request.MonoxidoOrdenMedica,
                request.MonoxidoResumenClinico),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/apartados/confirmaciones")]
    public async Task<IActionResult> ConfirmHold(Guid slotId, [FromBody] ConfirmHeldAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.ConfirmHoldAsync(
            User.GetUserId(),
            slotId,
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.HoldAppointmentCommand(
                request.PacienteId,
                request.EsMonoxido,
                request.ReferidoTercero,
                request.ReferenteId,
                request.ModalidadCobro,
                request.ObraSocialId,
                request.NumeroAutorizacion,
                request.SesionesAutorizadas,
                request.CicloObraSocialId,
                request.IniciarNuevoCicloObraSocial,
                request.ConvenioCorroborado,
                request.MedicoId,
                request.MedicoUserId,
                request.EsNuevoIngreso,
                request.MonoxidoOrdenMedica,
                request.MonoxidoResumenClinico),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/apartados/liberaciones")]
    public async Task<IActionResult> ReleaseHold(Guid slotId, [FromBody] ReleaseHeldAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.ReleaseHoldAsync(User.GetUserId(), slotId, GetRequiredIdempotencyKey(), request.Motivo, cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPost("bloques/asignaciones")]
    public async Task<IActionResult> AssignBlock([FromBody] AssignBlockAppointmentsRequest request, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.AssignBlockAsync(
            User.GetUserId(),
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.AssignBlockAppointmentsCommand(
                request.Fecha,
                request.Hora,
                request.CamaraId,
                request.PacienteId,
                request.EsTanda,
                request.TandaId,
                request.ReferidoTercero,
                request.ReferenteId,
                request.ModalidadCobro,
                request.ObraSocialId,
                request.NumeroAutorizacion,
                request.SesionesAutorizadas,
                request.CicloObraSocialId,
                request.IniciarNuevoCicloObraSocial,
                request.ConvenioCorroborado,
                request.MedicoId,
                request.MedicoUserId,
                request.EsNuevoIngreso,
                request.EsMonoxido,
                request.MonoxidoOrdenMedica,
                request.MonoxidoResumenClinico),
            cancellationToken);

        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("bloques/cancelaciones")]
    public async Task<IActionResult> CancelBlock([FromBody] CancelBlockAppointmentsRequest request, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.CancelBlockAsync(
            User.GetUserId(),
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.CancelBlockAppointmentsCommand(request.Fecha, request.Hora, request.CamaraId, request.PacienteId, request.Motivo),
            cancellationToken);

        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("tandas/{tandaId:guid}/cancelaciones")]
    public async Task<IActionResult> CancelTanda(Guid tandaId, [FromBody] CancelTandaRequest request, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.CancelTandaAsync(User.GetUserId(), tandaId, GetRequiredIdempotencyKey(), request.Motivo, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("tandas/disponibilidad")]
    public async Task<IActionResult> GetTandaAvailability([FromQuery(Name = "fecha_inicio")] DateOnly fechaInicio, [FromQuery(Name = "fecha_fin")] DateOnly fechaFin, [FromQuery(Name = "paciente_id")] Guid? pacienteId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetTandaAvailabilityAsync(fechaInicio, fechaFin, pacienteId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("tandas/disponibilidad/detalle")]
    public async Task<IActionResult> GetTandaAvailabilityDetail([FromQuery(Name = "fecha_inicio")] DateOnly fechaInicio, [FromQuery(Name = "fecha_fin")] DateOnly fechaFin, [FromQuery(Name = "paciente_id")] Guid? pacienteId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetTandaAvailabilityAggregatedAsync(fechaInicio, fechaFin, pacienteId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("tandas/{tandaId:guid}/slots")]
    public async Task<IActionResult> GetSlotsByTanda(Guid tandaId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetSlotsByTandaAsync(tandaId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("tandas/{tandaId:guid}/slots/activos")]
    public async Task<IActionResult> GetActiveSlotsByTanda(Guid tandaId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetActiveSlotsByTandaAsync(tandaId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("bloques/historial")]
    public async Task<IActionResult> GetBlockHistory([FromQuery] DateOnly fecha, [FromQuery] TimeOnly hora, [FromQuery(Name = "camara_id")] int? camaraId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetBlockHistoryAsync(fecha, hora, camaraId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("bloques/historial/slot/{slotId:guid}")]
    public async Task<IActionResult> GetBlockHistoryBySlot(Guid slotId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetBlockHistoryBySlotAsync(slotId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("bloques/historial/rango")]
    public async Task<IActionResult> GetBlockHistoryByRange([FromQuery(Name = "fecha_inicio")] DateOnly fechaInicio, [FromQuery(Name = "fecha_fin")] DateOnly fechaFin, [FromQuery(Name = "camara_id")] int? camaraId, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.GetBlockHistoryByRangeAsync(fechaInicio, fechaFin, camaraId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("bloques/historial")]
    [Authorize(Policy = "ConsultasManage")]
    public async Task<IActionResult> RegisterBlockHistory([FromBody] IReadOnlyCollection<RegisterBlockHistoryEntryRequest> request, CancellationToken cancellationToken)
    {
        var total = await appointmentsService.RegisterBlockHistoryAsync(
            User.GetUserId(),
            request.Select(entry => new BlockHistoryWriteCommand(entry.Fecha, entry.Hora, entry.CamaraId, entry.SlotId, entry.Lugar, entry.Accion, entry.PacienteId, entry.Motivo)).ToArray(),
            cancellationToken);

        return Ok(new { total });
    }

    [HttpPatch("{slotId:guid}/datos-operativos")]
    public async Task<IActionResult> UpdateOperative(Guid slotId, [FromBody] AppointmentOperativeRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.UpdateOperativeAsync(
            User.GetUserId(),
            slotId,
            new MedicalCenter.Application.DTOs.AppointmentOperativeCommand(
                request.ReferidoTercero,
                request.ReferenteId,
                request.ModalidadCobro,
                request.ObraSocialId,
                request.NumeroAutorizacion,
                request.SesionesAutorizadas,
                request.CicloObraSocialId,
                request.IniciarNuevoCicloObraSocial,
                request.ConvenioCorroborado,
                request.MedicoId,
                request.EsNuevoIngreso,
                request.EsMonoxido,
                request.MonoxidoOrdenMedica,
                request.MonoxidoResumenClinico,
                request.MedicoUserId),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    [HttpPatch("tandas/{tandaId:guid}/datos-operativos")]
    public async Task<IActionResult> UpdateOperativeByTanda(Guid tandaId, [FromBody] AppointmentOperativeRequest request, CancellationToken cancellationToken)
    {
        var items = await appointmentsService.UpdateOperativeByTandaAsync(
            User.GetUserId(),
            tandaId,
            new MedicalCenter.Application.DTOs.AppointmentOperativeCommand(
                request.ReferidoTercero,
                request.ReferenteId,
                request.ModalidadCobro,
                request.ObraSocialId,
                request.NumeroAutorizacion,
                request.SesionesAutorizadas,
                request.CicloObraSocialId,
                request.IniciarNuevoCicloObraSocial,
                request.ConvenioCorroborado,
                request.MedicoId,
                request.EsNuevoIngreso,
                request.EsMonoxido,
                request.MonoxidoOrdenMedica,
                request.MonoxidoResumenClinico),
            cancellationToken);

        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("{slotId:guid}/reprogramaciones/tanda")]
    public async Task<IActionResult> RescheduleTanda(Guid slotId, [FromBody] RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.RescheduleAsync(
            User.GetUserId(),
            slotId,
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.RescheduleAppointmentCommand(request.TargetSlotId, "tanda"),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    [HttpPost("{slotId:guid}/reprogramaciones/bloque")]
    public async Task<IActionResult> RescheduleBlock(Guid slotId, [FromBody] RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.RescheduleAsync(
            User.GetUserId(),
            slotId,
            GetRequiredIdempotencyKey(),
            new MedicalCenter.Application.DTOs.RescheduleAppointmentCommand(request.TargetSlotId, "bloque_tanda"),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    private string GetRequiredIdempotencyKey() =>
        Request.Headers.TryGetValue("Idempotency-Key", out var values)
            ? values.ToString()
            : string.Empty;
}
