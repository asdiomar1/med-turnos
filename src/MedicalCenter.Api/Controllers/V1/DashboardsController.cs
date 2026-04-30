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
        return Ok(Map(await dashboardService.GetResumenAsync(date, cancellationToken)));
    }

    [HttpGet("ocupacion")]
    public async Task<IActionResult> GetOcupacion([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(Map(await dashboardService.GetOcupacionAsync(date, cancellationToken)));
    }

    [HttpGet("agenda")]
    public async Task<IActionResult> GetAgenda([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetAgendaAsync(date, cancellationToken)).Select(Map));
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> GetAlertas([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetAlertasAsync(date, cancellationToken)).Select(Map));
    }

    [HttpGet("volumen-semanal")]
    public async Task<IActionResult> GetVolumenSemanal([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dashboardService.GetVolumenSemanalAsync(date, cancellationToken)).Select(Map));
    }

    private static DashboardSummaryResponse Map(MedicalCenter.Application.DTOs.DashboardSummaryDto x) => new()
    {
        Fecha = x.Fecha,
        TotalTurnos = x.TotalTurnos,
        Libres = x.Libres,
        Ocupados = x.Ocupados,
        Apartados = x.Apartados,
        Cancelados = x.Cancelados,
        OcupacionPorcentaje = x.OcupacionPorcentaje,
        GeneradoEn = x.GeneradoEn
    };

    private static DashboardOccupancyResponse Map(MedicalCenter.Application.DTOs.DashboardOccupancyDto x) => new()
    {
        Fecha = x.Fecha,
        TotalTurnos = x.TotalTurnos,
        Libres = x.Libres,
        Ocupados = x.Ocupados,
        Apartados = x.Apartados,
        Cancelados = x.Cancelados,
        OcupacionPorcentaje = x.OcupacionPorcentaje,
        PorCamara = x.PorCamara.Select(Map).ToArray()
    };

    private static DashboardOccupancyCameraResponse Map(MedicalCenter.Application.DTOs.DashboardOccupancyCameraDto x) => new()
    {
        CameraId = x.CameraId,
        CameraName = x.CameraName,
        TotalTurnos = x.TotalTurnos,
        Libres = x.Libres,
        Ocupados = x.Ocupados,
        Apartados = x.Apartados,
        Cancelados = x.Cancelados
    };

    private static DashboardAgendaBucketResponse Map(MedicalCenter.Application.DTOs.DashboardAgendaBucketDto x) => new()
    {
        Fecha = x.Fecha,
        Hora = x.Hora,
        CameraId = x.CameraId,
        CameraName = x.CameraName,
        TotalTurnos = x.TotalTurnos,
        Libres = x.Libres,
        Ocupados = x.Ocupados,
        Apartados = x.Apartados,
        Cancelados = x.Cancelados
    };

    private static DashboardAlertResponse Map(MedicalCenter.Application.DTOs.DashboardAlertDto x) => new()
    {
        Code = x.Code,
        Message = x.Message,
        Severity = x.Severity,
        Count = x.Count
    };

    private static DashboardWeeklyVolumeItemResponse Map(MedicalCenter.Application.DTOs.DashboardWeeklyVolumeItemDto x) => new()
    {
        Fecha = x.Fecha,
        TotalTurnos = x.TotalTurnos,
        Libres = x.Libres,
        Ocupados = x.Ocupados,
        Apartados = x.Apartados,
        Cancelados = x.Cancelados
    };
}
