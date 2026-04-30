using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.OutOfHoursTurns;
using MedicalCenter.Contracts.Consultations;
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
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OutOfHoursTurnCreateRequest request, CancellationToken cancellationToken)
    {
        var item = await outOfHoursTurnsService.CreateAsync(
            User.GetUserId(),
            new OutOfHoursTurnCreateCommand(request.Fecha, request.Hora, request.PacienteId, request.OperadorCamaraId, request.Notas, request.EsMonoxido, request.MonoxidoOrdenMedica, request.MonoxidoResumenClinico, request.MonoxidoMedicoId),
            GetRequiredIdempotencyKey(),
            cancellationToken);

        return Ok(item.ToResponse());
    }

    [HttpDelete("{turnoId:guid}")]
    public async Task<IActionResult> Cancel(Guid turnoId, CancellationToken cancellationToken)
    {
        var item = await outOfHoursTurnsService.CancelAsync(User.GetUserId(), turnoId, GetRequiredIdempotencyKey(), cancellationToken);
        return Ok(item.ToResponse());
    }

    private string GetRequiredIdempotencyKey() =>
        Request.Headers.TryGetValue("Idempotency-Key", out var values) ? values.ToString() : string.Empty;
}
