using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Contracts.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/whatsapp")]
[Authorize(Policy = "WhatsappDispatch")]
public sealed class TurnosWhatsAppController(IWhatsappService whatsappService) : ControllerBase
{
    [HttpPost("dispatch")]
    public async Task<IActionResult> Dispatch([FromBody] WhatsappDispatchRequest request, CancellationToken cancellationToken)
    {
        var result = await whatsappService.DispatchAsync(new WhatsappDispatchCommand(request.SlotIds, request.Limit), cancellationToken);
        return Ok(new WhatsappDispatchResponse
        {
            Requested = result.Requested,
            Found = result.Found
        });
    }

    [HttpPost("send-reminders-24h")]
    public async Task<IActionResult> SendReminders24h([FromBody] WhatsappReminderRequest request, CancellationToken cancellationToken)
    {
        var result = await whatsappService.SendRemindersAsync(new WhatsappReminderCommand(request.FechaObjetivo), cancellationToken);
        return Ok(new WhatsappReminderResponse
        {
            FechaObjetivo = result.FechaObjetivo,
            Total = result.Total
        });
    }
}
