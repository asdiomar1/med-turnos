using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.Features.Catalogs;
using MedicalCenter.Contracts.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/catalogos")]
[Authorize(Policy = "AdminAccess")]
public sealed class CatalogsController(ICatalogsService catalogsService) : ControllerBase
{
    [HttpGet("condiciones-iva")]
    public async Task<IActionResult> GetCondicionesIva([FromQuery(Name = "include_inactive")] bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok((await catalogsService.GetCondicionesIvaAsync(includeInactive, cancellationToken)).Select(Map));

    [HttpGet("obras-sociales")]
    public async Task<IActionResult> GetObrasSociales(CancellationToken cancellationToken) =>
        Ok((await catalogsService.GetObrasSocialesAsync(cancellationToken)).Select(Map));

    [HttpPost("obras-sociales")]
    public async Task<IActionResult> CreateObraSocial([FromBody] CreateObraSocialRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await catalogsService.CreateObraSocialAsync(User.GetUserId(), request.Nombre, request.TieneConvenio, request.Abreviatura, cancellationToken)));

    [HttpPatch("obras-sociales/{obraSocialId:int}")]
    public async Task<IActionResult> UpdateObraSocial(int obraSocialId, [FromBody] UpdateObraSocialRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await catalogsService.UpdateObraSocialAsync(User.GetUserId(), obraSocialId, request.Nombre, request.TieneConvenio, request.Abreviatura, cancellationToken)));

    [HttpPatch("obras-sociales/{obraSocialId:int}/estado")]
    public async Task<IActionResult> ToggleObraSocialActive(int obraSocialId, [FromBody] ToggleObraSocialActiveRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await catalogsService.SetObraSocialActiveAsync(User.GetUserId(), obraSocialId, request.Activa, cancellationToken)));

    [HttpPatch("obras-sociales/{obraSocialId:int}/convenio")]
    public async Task<IActionResult> ToggleObraSocialConvenio(int obraSocialId, [FromBody] ToggleObraSocialConvenioRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await catalogsService.SetObraSocialConvenioAsync(User.GetUserId(), obraSocialId, request.TieneConvenio, cancellationToken)));

    private static CondicionIvaResponse Map(MedicalCenter.Application.DTOs.CondicionIvaSummaryDto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Activo = x.Activo,
        Orden = x.Orden,
        CreatedAt = x.CreatedAt
    };

    private static ObraSocialResponse Map(MedicalCenter.Application.DTOs.ObraSocialSummaryDto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Activa = x.Activa,
        TieneConvenio = x.TieneConvenio,
        Orden = x.Orden,
        Abreviatura = x.Abreviatura,
        CreatedAt = x.CreatedAt
    };
}
