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
public sealed class CamposConfigController(ICamposConfigService camposConfigService) : ControllerBase
{
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
