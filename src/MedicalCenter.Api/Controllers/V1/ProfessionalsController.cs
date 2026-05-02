using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.Professionals;
using MedicalCenter.Contracts.Professionals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/profesionales")]
[Authorize]
public sealed class ProfessionalsController(IProfessionalsService professionalsService) : ControllerBase
{
    [HttpGet("medicos")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetMedicos(CancellationToken cancellationToken) =>
        Ok((await professionalsService.GetMedicosAsync(cancellationToken)).Select(x => x.ToResponse()));

    [HttpGet("referentes")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetReferentes(CancellationToken cancellationToken) =>
        Ok((await professionalsService.GetReferentesAsync(cancellationToken)).Select(x => x.ToResponse()));

    [HttpPost("referentes")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateReferente([FromBody] CreateReferenteRequest request, CancellationToken cancellationToken) =>
        Ok((await professionalsService.CreateReferenteAsync(User.GetUserId(), request.Nombre, request.Tipo, cancellationToken)).ToResponse());

    [HttpPatch("referentes/{id:int}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateReferente(int id, [FromBody] UpdateReferenteRequest request, CancellationToken cancellationToken) =>
        Ok((await professionalsService.UpdateReferenteAsync(User.GetUserId(), id, request.Nombre, request.Tipo, cancellationToken)).ToResponse());

    [HttpPatch("referentes/{id:int}/estado")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateReferenteEstado(int id, [FromBody] UpdateReferenteStatusRequest request, CancellationToken cancellationToken) =>
        Ok((await professionalsService.SetReferenteActiveAsync(User.GetUserId(), id, request.Activo, cancellationToken)).ToResponse());

    [HttpGet("operadores-camara")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetOperadores(CancellationToken cancellationToken) =>
        Ok((await professionalsService.GetOperadoresAsync(cancellationToken)).Select(x => x.ToResponse()));
}
