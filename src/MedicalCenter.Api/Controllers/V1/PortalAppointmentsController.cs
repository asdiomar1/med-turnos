using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
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
    public async Task<IActionResult> Reserve(Guid slotId, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.ReservePortalAsync(User.GetUserId(), slotId, idempotencyKey ?? string.Empty, cancellationToken);
        return Ok(item.ToPortalResponse());
    }

    [HttpPost("{slotId:guid}/cancelaciones")]
    public async Task<IActionResult> Cancel(Guid slotId, [FromBody] CancelAppointmentRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var item = await appointmentsService.CancelPortalAsync(User.GetUserId(), slotId, idempotencyKey ?? string.Empty, request.Motivo, cancellationToken);
        return Ok(item.ToPortalResponse());
    }
}
