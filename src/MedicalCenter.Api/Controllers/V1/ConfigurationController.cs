using MedicalCenter.Api.Extensions;
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
        Ok(Map(await workingDaysConfigService.GetAsync(cancellationToken)));

    [HttpPut("dias-laborables")]
    [Authorize(Policy = "ConfigHorariosManage")]
    public async Task<IActionResult> UpsertDiasLaborables([FromBody] UpsertDiasLaborablesConfigRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await workingDaysConfigService.UpsertAsync(request.DiasSemana, cancellationToken)));

    [HttpGet("whatsapp-message-settings")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetWhatsappMessageSettings(CancellationToken cancellationToken) =>
        Ok((await whatsappMessageSettingsService.GetAllAsync(cancellationToken)).Select(Map));

    [HttpPut("whatsapp-message-settings/{key}")]
    [Authorize(Policy = "WhatsappManage")]
    public async Task<IActionResult> UpsertWhatsappMessageSetting(string key, [FromBody] UpdateWhatsappMessageSettingRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await whatsappMessageSettingsService.UpsertAsync(key, request.MessageText, request.Active, cancellationToken)));

    [HttpGet("campos-config")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetCamposConfig(CancellationToken cancellationToken) =>
        Ok((await camposConfigService.GetAllAsync(cancellationToken)).Select(Map));

    [HttpPost("campos-config")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateCampoConfig([FromBody] CreateCampoConfigRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await camposConfigService.CreateAsync(User.GetUserId(), request.Nombre, request.Tipo, cancellationToken)));

    [HttpPatch("campos-config/{id:guid}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateCampoConfig(Guid id, [FromBody] UpdateCampoConfigRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await camposConfigService.UpdateAsync(User.GetUserId(), id, request.Nombre, request.Tipo, cancellationToken)));

    [HttpDelete("campos-config/{id:guid}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> DeleteCampoConfig(Guid id, CancellationToken cancellationToken)
    {
        await camposConfigService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }

    private static DiasLaborablesConfigResponse Map(MedicalCenter.Application.DTOs.DiasLaborablesConfigDto x) => new()
    {
        Key = x.Key,
        DiasSemana = x.DiasSemana
    };

    private static WhatsappMessageSettingResponse Map(MedicalCenter.Application.DTOs.WhatsappMessageSettingDto x) => new()
    {
        Key = x.Key,
        Label = x.Label,
        Description = x.Description,
        MessageText = x.MessageText,
        Active = x.Active,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static CampoConfigResponse Map(MedicalCenter.Application.DTOs.CampoConfigSummaryDto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Tipo = x.Tipo,
        Orden = x.Orden,
        CreatedAt = x.CreatedAt
    };
}
