using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Contracts.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/portal/turnos")]
[Authorize]
public sealed class PortalAppointmentsController(IAppointmentsService appointmentsService) : ControllerBase
{
    [HttpPost("{slotId:guid}/reservas")]
    public async Task<IActionResult> Reserve(Guid slotId, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.ReservePortalAsync(User.GetUserId(), slotId, GetIdempotencyKey(), cancellationToken);
        return Ok(Map(item));
    }

    [HttpPost("{slotId:guid}/cancelaciones")]
    public async Task<IActionResult> Cancel(Guid slotId, [FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.CancelPortalAsync(User.GetUserId(), slotId, GetIdempotencyKey(), request.Motivo, cancellationToken);
        return Ok(Map(item));
    }

    private string? GetIdempotencyKey() =>
        Request.Headers.TryGetValue("Idempotency-Key", out var values)
            ? values.ToString()
            : null;

    private static AppointmentResponse Map(MedicalCenter.Application.DTOs.AppointmentSummary x) => new()
    {
        Id = x.Id,
        Fecha = x.Fecha,
        Hora = x.Hora,
        Lugar = x.Lugar,
        Estado = x.Estado,
        PacienteId = x.PacienteId,
        CamaraId = x.CamaraId,
        BlockId = x.BlockId,
        TandaId = x.TandaId,
        ApartadoPorUserId = x.ApartadoPorUserId,
        ApartadoTs = x.ApartadoTs
    };
}
