using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.OutOfHoursTurns;
using MedicalCenter.Contracts.Consultations;
using MedicalCenter.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/turnos/fuera-horario")]
[Authorize(Policy = "TurnosFueraHorarioManage")]
public sealed class OutOfHoursTurnsController(IOutOfHoursTurnsService outOfHoursTurnsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly fecha, CancellationToken cancellationToken)
    {
        var items = await outOfHoursTurnsService.GetByDateAsync(fecha, cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OutOfHoursTurnCreateRequest request, CancellationToken cancellationToken)
    {
        var item = await outOfHoursTurnsService.CreateAsync(
            User.GetUserId(),
            new OutOfHoursTurnCreateCommand(request.Fecha, request.Hora, request.PacienteId, request.OperadorCamaraId, request.Notas, request.EsMonoxido, request.MonoxidoOrdenMedica, request.MonoxidoResumenClinico, request.MonoxidoMedicoId),
            GetRequiredIdempotencyKey(),
            cancellationToken);

        return Ok(Map(item));
    }

    [HttpDelete("{turnoId:guid}")]
    public async Task<IActionResult> Cancel(Guid turnoId, CancellationToken cancellationToken)
    {
        var item = await outOfHoursTurnsService.CancelAsync(User.GetUserId(), turnoId, GetRequiredIdempotencyKey(), cancellationToken);
        return Ok(Map(item));
    }

    private string GetRequiredIdempotencyKey() =>
        Request.Headers.TryGetValue("Idempotency-Key", out var values) ? values.ToString() : string.Empty;

    private static OutOfHoursTurnResponse Map(OutOfHoursTurnSummary x) => new()
    {
        Id = x.Id,
        Fecha = x.Fecha,
        Hora = x.Hora,
        PacienteId = x.PacienteId,
        Notas = x.Notas,
        CreadoPor = x.CreadoPor,
        OperadorCamaraId = x.OperadorCamaraId,
        CreatedAt = x.CreatedAt,
        EsMonoxido = x.EsMonoxido,
        MonoxidoOrdenMedica = x.MonoxidoOrdenMedica,
        MonoxidoResumenClinico = x.MonoxidoResumenClinico,
        MonoxidoMedicoId = x.MonoxidoMedicoId,
        Paciente = x.Paciente is null ? null : new GuidLookupResponse { Id = x.Paciente.Id, Nombre = x.Paciente.Nombre, DocumentoIdentidad = x.Paciente.DocumentoIdentidad, Email = x.Paciente.Email, Activo = x.Paciente.Activo },
        MonoxidoMedico = x.MonoxidoMedico is null ? null : new IntLookupResponse { Id = x.MonoxidoMedico.Id, Nombre = x.MonoxidoMedico.Nombre, Extra = x.MonoxidoMedico.Extra, Activo = x.MonoxidoMedico.Activo },
        OperadorCamara = x.OperadorCamara is null ? null : new GuidLookupResponse { Id = x.OperadorCamara.Id, Nombre = x.OperadorCamara.Nombre, DocumentoIdentidad = x.OperadorCamara.DocumentoIdentidad, Email = x.OperadorCamara.Email, Activo = x.OperadorCamara.Activo }
    };
}
