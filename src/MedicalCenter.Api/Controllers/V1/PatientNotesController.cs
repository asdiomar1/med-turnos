using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.Features.PatientNotes;
using MedicalCenter.Contracts.PatientNotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/pacientes/{pacienteId:guid}/notas")]
[Authorize(Policy = "ClinicalHistoryRead")]
public sealed class PatientNotesController(IPatientNotesService patientNotesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid pacienteId, CancellationToken cancellationToken)
    {
        var items = await patientNotesService.GetByPatientAsync(pacienteId, cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpPost]
    [Authorize(Policy = "ClinicalHistoryManage")]
    public async Task<IActionResult> Create(Guid pacienteId, [FromBody] CreatePatientNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await patientNotesService.CreateAsync(User.GetUserId(), pacienteId, request.Mensaje, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Map(result));
    }

    [HttpDelete("{notaId:guid}")]
    [Authorize(Policy = "ClinicalHistoryManage")]
    public async Task<IActionResult> Delete(Guid pacienteId, Guid notaId, CancellationToken cancellationToken)
    {
        await patientNotesService.DeleteAsync(User.GetUserId(), notaId, cancellationToken);
        return NoContent();
    }

    private static PatientNoteResponse Map(MedicalCenter.Application.DTOs.PatientNoteSummary x) => new()
    {
        Id = x.Id,
        PatientId = x.PatientId,
        AuthorId = x.AuthorId,
        Message = x.Message,
        CreatedAt = x.CreatedAt
    };
}
