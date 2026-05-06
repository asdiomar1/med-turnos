using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.Configuration;
using MedicalCenter.Contracts.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/configuracion")]
[Authorize]
public sealed class WhatsappMessageSettingsController(IWhatsappMessageSettingsService whatsappMessageSettingsService) : ControllerBase
{
    [HttpGet("whatsapp-message-settings")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetWhatsappMessageSettings(CancellationToken cancellationToken) =>
        Ok((await whatsappMessageSettingsService.GetAllAsync(cancellationToken)).Select(x => x.ToResponse()));

    [HttpPut("whatsapp-message-settings/{key}")]
    [Authorize(Policy = "WhatsappManage")]
    public async Task<IActionResult> UpsertWhatsappMessageSetting(string key, [FromBody] UpdateWhatsappMessageSettingRequest request, CancellationToken cancellationToken) =>
        Ok((await whatsappMessageSettingsService.UpsertAsync(key, request.MessageText, request.Active, cancellationToken)).ToResponse());
}
