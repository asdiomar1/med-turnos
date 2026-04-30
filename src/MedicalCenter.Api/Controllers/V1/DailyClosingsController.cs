using System.Text.Json;
using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.Features.DailyClosings;
using MedicalCenter.Contracts.Dashboards;
using MedicalCenter.Contracts.DailyClosings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/cierres-diarios")]
[Authorize(Policy = "StaffRead")]
public sealed class DailyClosingsController(IDailyClosingsService dailyClosingsService) : ControllerBase
{
    [HttpGet("estado")]
    public async Task<IActionResult> GetState([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(Map(await dailyClosingsService.GetDetailAsync(date, null, cancellationToken)));
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewDailyClosingRequest request, CancellationToken cancellationToken)
    {
        request ??= new PreviewDailyClosingRequest();
        var result = await dailyClosingsService.PreviewAsync(request.Fecha ?? DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        return Ok(new DailyClosingPreviewResponse
        {
            Fecha = result.Fecha,
            TotalTurnos = result.TotalTurnos,
            Libres = result.Libres,
            Ocupados = result.Ocupados,
            Apartados = result.Apartados,
            Cancelados = result.Cancelados,
            OcupacionPorcentaje = result.OcupacionPorcentaje,
            AptoParaCierre = result.AptoParaCierre,
            Alertas = result.Alertas.Select(Map).ToArray(),
            GeneradoEn = result.GeneradoEn
        });
    }

    [HttpPost("confirmar")]
    [Authorize(Policy = "StaffManage")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmDailyClosingRequest request, [FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        request ??= new ConfirmDailyClosingRequest();
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await dailyClosingsService.ConfirmAsync(User.GetUserId(), date, request.Detalles.HasValue ? request.Detalles.Value.GetRawText() : null, cancellationToken);
        return Ok(Map(result));
    }

    [HttpGet("detalle")]
    public async Task<IActionResult> Detail([FromQuery] DateOnly? fecha, [FromQuery(Name = "cierre_id")] Guid? closingId, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(Map(await dailyClosingsService.GetDetailAsync(date, closingId, cancellationToken)));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateOnly? fecha, [FromQuery(Name = "cierre_id")] Guid? closingId, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(Map(await dailyClosingsService.GetDetailAsync(date, closingId, cancellationToken)));
    }

    [HttpGet("export/mensual")]
    public async Task<IActionResult> ExportMonthly([FromQuery] int anio, [FromQuery] int mes, CancellationToken cancellationToken)
    {
        var items = await dailyClosingsService.GetMonthlyExportAsync(anio, mes, cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpPost("reabrir")]
    [Authorize(Policy = "StaffManage")]
    public async Task<IActionResult> Reopen([FromBody] ReopenDailyClosingRequest request, [FromQuery] DateOnly? fecha, [FromQuery(Name = "cierre_id")] Guid? closingId, CancellationToken cancellationToken)
    {
        request ??= new ReopenDailyClosingRequest();
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(Map(await dailyClosingsService.ReopenAsync(User.GetUserId(), date, closingId, request.Motivo, cancellationToken)));
    }

    private static DailyClosingResponse Map(MedicalCenter.Application.DTOs.DailyClosingSummaryDto x) => new()
    {
        Id = x.Id,
        Fecha = x.Fecha,
        Estado = x.Estado,
        Detalles = ParseJson(x.DetallesJson),
        CreatedByUserId = x.CreatedByUserId,
        ConfirmedByUserId = x.ConfirmedByUserId,
        ReopenedByUserId = x.ReopenedByUserId,
        MotivoReapertura = x.MotivoReapertura,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        ConfirmedAt = x.ConfirmedAt,
        ReopenedAt = x.ReopenedAt
    };

    private static DashboardAlertResponse Map(MedicalCenter.Application.DTOs.DashboardAlertDto x) => new()
    {
        Code = x.Code,
        Message = x.Message,
        Severity = x.Severity,
        Count = x.Count
    };

    private static JsonElement? ParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.Clone();
    }

}
