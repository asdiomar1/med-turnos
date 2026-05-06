using MedicalCenter.Application.Features.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/whatsapp/webhook")]
[AllowAnonymous]
public sealed class WhatsAppWebhookReceiverController(IWhatsappWebhookProcessor webhookProcessor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var result = await webhookProcessor.ProcessAsync(payload, cancellationToken);
        return Ok(new
        {
            stored = result.Stored,
            processed = result.Processed,
            event_type = result.EventType,
            entry_id = result.EntryId,
            message_id = result.MessageId
        });
    }
}
