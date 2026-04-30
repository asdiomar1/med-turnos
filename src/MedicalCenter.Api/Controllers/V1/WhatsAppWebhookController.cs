using MedicalCenter.Application.Features.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/whatsapp/webhook")]
[AllowAnonymous]
public sealed class WhatsAppWebhookController(IWhatsappWebhookProcessor webhookProcessor, IOptions<MedicalCenter.Infrastructure.Options.WhatsAppOptions> options) : ControllerBase
{
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var configuredToken = options.Value.WebhookVerifyToken;

        if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Modo de verificacion invalido." });
        }

        if (string.IsNullOrWhiteSpace(challenge))
        {
            return BadRequest(new { error = "Challenge requerido." });
        }

        if (!string.Equals(verifyToken, configuredToken, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Token de verificacion invalido." });
        }

        return Content(challenge, "text/plain");
    }

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
