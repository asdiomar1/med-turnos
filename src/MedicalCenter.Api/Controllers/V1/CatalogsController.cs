using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.Catalogs;
using MedicalCenter.Contracts.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/catalogos")]
public sealed class CatalogsController(ICatalogsService catalogsService) : ControllerBase
{
    [HttpGet("condiciones-iva")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetCondicionesIva([FromQuery(Name = "include_inactive")] bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok((await catalogsService.GetCondicionesIvaAsync(includeInactive, cancellationToken)).Select(x => x.ToResponse()));

    [HttpPost("condiciones-iva")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateCondicionIva([FromBody] CreateCondicionIvaRequest request, CancellationToken cancellationToken) =>
        Ok((await catalogsService.CreateCondicionIvaAsync(User.GetUserId(), request.Nombre, cancellationToken)).ToResponse());

    [HttpPatch("condiciones-iva/{condicionIvaId:int}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateCondicionIva(int condicionIvaId, [FromBody] UpdateCondicionIvaRequest request, CancellationToken cancellationToken) =>
        Ok((await catalogsService.UpdateCondicionIvaAsync(User.GetUserId(), condicionIvaId, request.Nombre, request.Orden, cancellationToken)).ToResponse());

    [HttpPatch("condiciones-iva/{condicionIvaId:int}/estado")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> ToggleCondicionIvaActive(int condicionIvaId, [FromBody] ToggleCondicionIvaActiveRequest request, CancellationToken cancellationToken) =>
        Ok((await catalogsService.SetCondicionIvaActiveAsync(User.GetUserId(), condicionIvaId, request.Activo, cancellationToken)).ToResponse());

    [HttpGet("obras-sociales")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetObrasSociales(CancellationToken cancellationToken) =>
        Ok((await catalogsService.GetObrasSocialesAsync(cancellationToken)).Select(x => x.ToResponse()));

    [HttpPost("obras-sociales")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateObraSocial([FromBody] CreateObraSocialRequest request, CancellationToken cancellationToken) =>
        Ok((await catalogsService.CreateObraSocialAsync(User.GetUserId(), request.Nombre, request.TieneConvenio, request.Abreviatura, cancellationToken)).ToResponse());

    [HttpPatch("obras-sociales/{obraSocialId:int}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateObraSocial(int obraSocialId, [FromBody] UpdateObraSocialRequest request, CancellationToken cancellationToken) =>
        Ok((await catalogsService.UpdateObraSocialAsync(User.GetUserId(), obraSocialId, request.Nombre, request.TieneConvenio, request.Abreviatura, cancellationToken)).ToResponse());

    [HttpPatch("obras-sociales/{obraSocialId:int}/estado")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> ToggleObraSocialActive(int obraSocialId, [FromBody] ToggleObraSocialActiveRequest request, CancellationToken cancellationToken) =>
        Ok((await catalogsService.SetObraSocialActiveAsync(User.GetUserId(), obraSocialId, request.Activa, cancellationToken)).ToResponse());

    [HttpPatch("obras-sociales/{obraSocialId:int}/convenio")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> ToggleObraSocialConvenio(int obraSocialId, [FromBody] ToggleObraSocialConvenioRequest request, CancellationToken cancellationToken) =>
        Ok((await catalogsService.SetObraSocialConvenioAsync(User.GetUserId(), obraSocialId, request.TieneConvenio, cancellationToken)).ToResponse());

}
