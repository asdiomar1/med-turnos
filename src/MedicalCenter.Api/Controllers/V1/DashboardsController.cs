using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.Dashboards;
using MedicalCenter.Contracts.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/dashboards")]
[Authorize(Policy = "StaffRead")]
public sealed class DashboardsController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetResumenAsync(date, cancellationToken)).ToResponse());
    }

    [HttpGet("ocupacion")]
    public async Task<IActionResult> GetOcupacion([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetOcupacionAsync(date, cancellationToken)).ToResponse());
    }

    [HttpGet("agenda")]
    public async Task<IActionResult> GetAgenda([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetAgendaAsync(date, cancellationToken)).Select(x => x.ToResponse()));
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> GetAlertas([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetAlertasAsync(date, cancellationToken)).Select(x => x.ToResponse()));
    }

    [HttpGet("volumen-semanal")]
    public async Task<IActionResult> GetVolumenSemanal([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetVolumenSemanalAsync(date, cancellationToken)).Select(x => x.ToResponse()));
    }
}
