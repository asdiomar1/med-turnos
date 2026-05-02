using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.ClinicalHistory;
using MedicalCenter.Contracts.ClinicalHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/pacientes/{pacienteId:guid}/historia-clinica")]
[Authorize]
public sealed class ClinicalHistoryController(IClinicalHistoryService clinicalHistoryService) : ControllerBase
{
    [HttpGet("/api/v1/historias-clinicas/resumen")]
    [Authorize(Policy = "ClinicalHistoryRead")]
    public async Task<IActionResult> GetResumen(CancellationToken cancellationToken)
    {
        var items = await clinicalHistoryService.GetResumenAsync(cancellationToken);
        return Ok(items.Select(x => x.ToResumenResponse()));
    }

    [HttpGet]
    [Authorize(Policy = "ClinicalHistoryRead")]
    public async Task<IActionResult> Get(Guid pacienteId, CancellationToken cancellationToken)
    {
        var item = await clinicalHistoryService.GetAsync(pacienteId, cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPatch]
    [Authorize(Policy = "ClinicalHistoryManage")]
    public async Task<IActionResult> Update(Guid pacienteId, [FromBody] UpdateClinicalHistoryRequest request, CancellationToken cancellationToken)
    {
        var item = await clinicalHistoryService.UpdateAsync(User.GetUserId(), pacienteId, request.Antecedentes, request.Alergias, request.MedicacionActual, request.ObservacionesRelevantes, cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpPatch("numero")]
    [Authorize(Policy = "ClinicalHistoryManage")]
    public async Task<IActionResult> UpdateNumero(Guid pacienteId, [FromBody] UpdateClinicalHistoryNumberRequest request, CancellationToken cancellationToken)
    {
        var item = await clinicalHistoryService.UpdateNumeroAsync(User.GetUserId(), pacienteId, request.Numero, cancellationToken);
        return Ok(item.ToResponse());
    }

    [HttpGet("evoluciones")]
    [Authorize(Policy = "ClinicalHistoryRead")]
    public async Task<IActionResult> GetEvolutions(Guid pacienteId, CancellationToken cancellationToken)
    {
        var items = await clinicalHistoryService.GetEvolutionsAsync(pacienteId, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("evoluciones")]
    [Authorize(Policy = "ClinicalHistoryManage")]
    public async Task<IActionResult> CreateEvolution(Guid pacienteId, [FromBody] CreateClinicalEvolutionRequest request, CancellationToken cancellationToken)
    {
        var item = await clinicalHistoryService.CreateEvolutionAsync(User.GetUserId(), pacienteId, request.MedicoId, request.FechaClinica, request.Titulo, request.Nota, request.DiagnosticoImpresion, request.Indicaciones, request.ConsultaSlotId, cancellationToken, request.MedicoUserId);
        return StatusCode(StatusCodes.Status201Created, item.ToResponse());
    }
}
