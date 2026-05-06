using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.Configuration;
using MedicalCenter.Contracts.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/configuracion")]
[Authorize]
public sealed class WorkingDaysConfigController(IWorkingDaysConfigService workingDaysConfigService) : ControllerBase
{
    [HttpGet("dias-laborables")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetDiasLaborables(CancellationToken cancellationToken) =>
        Ok((await workingDaysConfigService.GetAsync(cancellationToken)).ToResponse());

    [HttpPut("dias-laborables")]
    [Authorize(Policy = "ConfigHorariosManage")]
    public async Task<IActionResult> UpsertDiasLaborables([FromBody] UpsertDiasLaborablesConfigRequest request, CancellationToken cancellationToken) =>
        Ok((await workingDaysConfigService.UpsertAsync(request.DiasSemana, cancellationToken)).ToResponse());
}
