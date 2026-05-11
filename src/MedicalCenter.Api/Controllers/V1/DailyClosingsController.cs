using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.DailyClosings;
using MedicalCenter.Contracts.Common;
using MedicalCenter.Contracts.DailyClosings;
using MedicalCenter.Contracts.Dashboards;
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
        return Ok((await dailyClosingsService.GetDetailAsync(date, null, cancellationToken)).ToResponse());
    }

    [HttpGet("preview")]
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] PreviewDailyClosingRequest? request, [FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var date = request?.Fecha ?? fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await dailyClosingsService.PreviewAsync(date, cancellationToken);
        return Ok(new DataResponse<DailyClosingPreviewResponse> { Data = result.ToResponse() });
    }

    [HttpPost("confirmar")]
    [Authorize(Policy = "StaffManage")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmDailyClosingRequest request, [FromQuery] DateOnly? fecha, [FromQuery(Name = "cierre_id")] Guid? closingId, CancellationToken cancellationToken)
    {
        request ??= new ConfirmDailyClosingRequest();
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await dailyClosingsService.ConfirmAsync(User.GetUserId(), date, closingId, request.Detalles.HasValue ? request.Detalles.Value.GetRawText() : null, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("detalle")]
    public async Task<IActionResult> Detail([FromQuery] DateOnly? fecha, [FromQuery(Name = "cierre_id")] Guid? closingId, CancellationToken cancellationToken)
    {
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dailyClosingsService.GetDetailAsync(date, closingId, cancellationToken)).ToResponse());
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateOnly? fecha, [FromQuery(Name = "cierre_id")] Guid? closingId, CancellationToken cancellationToken) =>
        await Detail(fecha, closingId, cancellationToken);

    [HttpGet("export/mensual")]
    public async Task<IActionResult> ExportMonthly([FromQuery] int anio, [FromQuery] int mes, CancellationToken cancellationToken)
    {
        var items = await dailyClosingsService.GetMonthlyExportAsync(anio, mes, cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("reabrir")]
    [Authorize(Policy = "StaffManage")]
    public async Task<IActionResult> Reopen([FromBody] ReopenDailyClosingRequest request, [FromQuery] DateOnly? fecha, [FromQuery(Name = "cierre_id")] Guid? closingId, CancellationToken cancellationToken)
    {
        request ??= new ReopenDailyClosingRequest();
        var date = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok((await dailyClosingsService.ReopenAsync(User.GetUserId(), date, closingId, request.Motivo, cancellationToken)).ToResponse());
    }
}
