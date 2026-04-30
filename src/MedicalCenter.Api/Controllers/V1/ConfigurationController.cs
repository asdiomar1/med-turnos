using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.Configuration;
using MedicalCenter.Contracts.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/configuracion")]
[Authorize]
public sealed class ConfigurationController(
    IWorkingDaysConfigService workingDaysConfigService,
    IWhatsappMessageSettingsService whatsappMessageSettingsService,
    ICamposConfigService camposConfigService) : ControllerBase
{
    [HttpGet("dias-laborables")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetDiasLaborables(CancellationToken cancellationToken) =>
        Ok((await workingDaysConfigService.GetAsync(cancellationToken)).ToResponse());

    [HttpPut("dias-laborables")]
    [Authorize(Policy = "ConfigHorariosManage")]
    public async Task<IActionResult> UpsertDiasLaborables([FromBody] UpsertDiasLaborablesConfigRequest request, CancellationToken cancellationToken) =>
        Ok((await workingDaysConfigService.UpsertAsync(request.DiasSemana, cancellationToken)).ToResponse());

    [HttpGet("whatsapp-message-settings")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetWhatsappMessageSettings(CancellationToken cancellationToken) =>
        Ok((await whatsappMessageSettingsService.GetAllAsync(cancellationToken)).Select(x => x.ToResponse()));

    [HttpPut("whatsapp-message-settings/{key}")]
    [Authorize(Policy = "WhatsappManage")]
    public async Task<IActionResult> UpsertWhatsappMessageSetting(string key, [FromBody] UpdateWhatsappMessageSettingRequest request, CancellationToken cancellationToken) =>
        Ok((await whatsappMessageSettingsService.UpsertAsync(key, request.MessageText, request.Active, cancellationToken)).ToResponse());

    [HttpGet("campos-config")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetCamposConfig(CancellationToken cancellationToken) =>
        Ok((await camposConfigService.GetAllAsync(cancellationToken)).Select(x => x.ToResponse()));

    [HttpPost("campos-config")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateCampoConfig([FromBody] CreateCampoConfigRequest request, CancellationToken cancellationToken) =>
        Ok((await camposConfigService.CreateAsync(User.GetUserId(), request.Nombre, request.Tipo, cancellationToken)).ToResponse());

    [HttpPatch("campos-config/{id:guid}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateCampoConfig(Guid id, [FromBody] UpdateCampoConfigRequest request, CancellationToken cancellationToken) =>
        Ok((await camposConfigService.UpdateAsync(User.GetUserId(), id, request.Nombre, request.Tipo, cancellationToken)).ToResponse());

    [HttpDelete("campos-config/{id:guid}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> DeleteCampoConfig(Guid id, CancellationToken cancellationToken)
    {
        await camposConfigService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
