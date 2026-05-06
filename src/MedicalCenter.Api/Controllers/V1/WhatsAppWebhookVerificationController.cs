using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/whatsapp/webhook")]
[AllowAnonymous]
public sealed class WhatsAppWebhookVerificationController(IOptions<MedicalCenter.Infrastructure.Options.WhatsAppOptions> options) : ControllerBase
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
}
