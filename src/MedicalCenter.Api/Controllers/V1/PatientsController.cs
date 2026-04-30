using MedicalCenter.Application.Features.Patients;
using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Filters;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Contracts.Common;
using MedicalCenter.Contracts.Patients;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/pacientes")]
[Authorize]
public sealed class PatientsController(IPatientsService patientsService, ISecurityAuditLogger auditLogger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery(Name = "include_inactive")] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var items = await patientsService.GetAsync(search, includeInactive, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await patientsService.CreateAsync(
            request.Nombre,
            request.Email,
            request.Telefono,
            request.DocumentoIdentidad,
            request.LoginIdentifier,
            request.Nacionalidad,
            request.CondicionIvaId,
            request.ObraSocialId,
            request.NumeroCredencialObraSocial,
            request.PortalHabilitado,
            request.OptInWhatsapp,
            request.OptInSource,
            request.Claustrofobico,
            request.Notas,
            JsonSerializer.Serialize(request.DatosExtra ?? new { }),
            cancellationToken);

        LogMutation("create_patient", result.Id.ToString());
        return StatusCode(StatusCodes.Status201Created, new DataResponse<object>
        {
            Data = new { id = result.Id, nombre = result.Nombre },
            Error = null
        });
    }

    [HttpPatch("{pacienteId:guid}")]
    [ServiceFilter(typeof(OwnershipFilter))]
    public async Task<IActionResult> Update(Guid pacienteId, [FromBody] UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await patientsService.UpdateAsync(
            pacienteId,
            request.Email,
            request.Telefono,
            request.DocumentoIdentidad,
            request.Nacionalidad,
            request.CondicionIvaId,
            request.ObraSocialId,
            request.NumeroCredencialObraSocial,
            request.Claustrofobico,
            request.Notas,
            JsonSerializer.Serialize(request.DatosExtra ?? new { }),
            request.ActualizarNotas,
            request.OptInWhatsapp,
            request.OptInSource,
            cancellationToken);

        LogMutation("update_patient", pacienteId.ToString());
        return Ok(result.ToResponse());
    }

    [HttpDelete("{pacienteId:guid}")]
    [ServiceFilter(typeof(OwnershipFilter))]
    public async Task<IActionResult> Delete(Guid pacienteId, CancellationToken cancellationToken)
    {
        var result = await patientsService.DeleteAsync(pacienteId, cancellationToken);
        LogMutation("delete_patient", pacienteId.ToString());
        return Ok(new OkResponse { Ok = result.Ok });
    }

    [HttpPatch("{pacienteId:guid}/portal")]
    [ServiceFilter(typeof(OwnershipFilter))]
    public async Task<IActionResult> ConfigurePortal(Guid pacienteId, [FromBody] UpdatePatientPortalRequest request, CancellationToken cancellationToken)
    {
        var result = await patientsService.ConfigurePortalAsync(pacienteId, request.PortalHabilitado, cancellationToken);
        LogMutation("configure_portal", pacienteId.ToString());
        return Ok(result.ToResponse());
    }

    [HttpPost("{pacienteId:guid}/portal/reset-enable")]
    [ServiceFilter(typeof(OwnershipFilter))]
    public async Task<IActionResult> EnableReset(Guid pacienteId, CancellationToken cancellationToken)
    {
        var result = await patientsService.EnableResetAsync(pacienteId, cancellationToken);
        LogMutation("enable_reset", pacienteId.ToString());
        return Ok(result.ToResponse());
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateMyPatientRequest request, CancellationToken cancellationToken)
    {
        var result = await patientsService.UpdateMyDataAsync(User.GetUserId(), request.Nombre, request.Email, request.Telefono, cancellationToken);
        LogMutation("update_my_data", User.GetUserId().ToString());
        return Ok(result.ToResponse());
    }

    private void LogMutation(string action, string targetId)
    {
        auditLogger.LogAsync(new SecurityEvent(
            EventType: "data_mutation",
            Message: $"Patient data mutation: {action}",
            UserId: User.GetUserId().ToString(),
            Path: HttpContext.Request.Path,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        ));
    }
}
