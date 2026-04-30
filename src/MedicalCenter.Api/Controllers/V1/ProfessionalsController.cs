using MedicalCenter.Api.Extensions;
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
        Ok((await professionalsService.GetMedicosAsync(cancellationToken)).Select(Map));

    [HttpPost("medicos")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateMedico([FromBody] CreateMedicoRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await professionalsService.CreateMedicoAsync(User.GetUserId(), request.Nombre, cancellationToken)));

    [HttpPatch("medicos/{id:int}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateMedico(int id, [FromBody] UpdateMedicoRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await professionalsService.UpdateMedicoAsync(User.GetUserId(), id, request.Nombre, cancellationToken)));

    [HttpPatch("medicos/{id:int}/estado")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateMedicoEstado(int id, [FromBody] UpdateMedicoStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await professionalsService.SetMedicoActiveAsync(User.GetUserId(), id, request.Activo, cancellationToken)));

    [HttpGet("referentes")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetReferentes(CancellationToken cancellationToken) =>
        Ok((await professionalsService.GetReferentesAsync(cancellationToken)).Select(Map));

    [HttpPost("referentes")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateReferente([FromBody] CreateReferenteRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await professionalsService.CreateReferenteAsync(User.GetUserId(), request.Nombre, request.Tipo, cancellationToken)));

    [HttpPatch("referentes/{id:int}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateReferente(int id, [FromBody] UpdateReferenteRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await professionalsService.UpdateReferenteAsync(User.GetUserId(), id, request.Nombre, request.Tipo, cancellationToken)));

    [HttpPatch("referentes/{id:int}/estado")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateReferenteEstado(int id, [FromBody] UpdateReferenteStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await professionalsService.SetReferenteActiveAsync(User.GetUserId(), id, request.Activo, cancellationToken)));

    [HttpGet("operadores-camara")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetOperadores(CancellationToken cancellationToken) =>
        Ok((await professionalsService.GetOperadoresAsync(cancellationToken)).Select(Map));

    private static MedicoResponse Map(MedicalCenter.Application.DTOs.MedicoSummaryDto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Activo = x.Activo,
        Orden = x.Orden,
        CreatedAt = x.CreatedAt,
        PerfilId = x.PerfilId
    };

    private static ReferenteResponse Map(MedicalCenter.Application.DTOs.ReferenteSummaryDto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Tipo = x.Tipo,
        Activo = x.Activo,
        Orden = x.Orden,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static OperadorCamaraResponse Map(MedicalCenter.Application.DTOs.OperadorCamaraSummaryDto x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        IsActive = x.IsActive
    };
}
