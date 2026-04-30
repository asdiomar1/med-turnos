using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Features.Schedules;
using MedicalCenter.Contracts.Common;
using MedicalCenter.Contracts.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/agendas")]
[Authorize]
public sealed class SchedulesController(ISchedulesService schedulesService) : ControllerBase
{
    [HttpGet("camaras")]
    public async Task<IActionResult> GetCamaras(CancellationToken cancellationToken)
    {
        var items = await schedulesService.GetCamarasAsync(cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("horarios")]
    public async Task<IActionResult> GetHorarios(CancellationToken cancellationToken)
    {
        var items = await schedulesService.GetHorariosAsync(cancellationToken);
        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpPost("camaras")]
    public async Task<IActionResult> CreateCamara([FromBody] CreateCameraRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.CreateCamaraAsync(User.GetUserId(), request.Nombre, request.Capacidad, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPatch("camaras/{id:int}")]
    public async Task<IActionResult> UpdateCamara(int id, [FromBody] UpdateCameraRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.UpdateCamaraAsync(User.GetUserId(), id, request.Nombre, request.Capacidad, cancellationToken);
        return Ok(new CameraMutationResponse
        {
            Camara = result.Camara.ToResponse(),
            Movidos = result.Movidos,
            Cancelados = result.Cancelados,
            ApartadosLiberados = result.ApartadosLiberados,
            Eliminados = result.Eliminados
        });
    }

    [HttpPatch("camaras/{id:int}/estado")]
    public async Task<IActionResult> UpdateCamaraEstado(int id, [FromBody] UpdateCameraStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.UpdateCamaraEstadoAsync(User.GetUserId(), id, request.Activa, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPost("horarios")]
    public async Task<IActionResult> CreateHorario([FromBody] CreateScheduleHourRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.CreateHorarioAsync(request.Hora, request.Orden, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPatch("horarios/{id:int}")]
    public async Task<IActionResult> UpdateHorario(int id, [FromBody] UpdateScheduleHourRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.UpdateHorarioAsync(id, request.Hora, request.Orden, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPatch("horarios/{id:int}/estado")]
    public async Task<IActionResult> UpdateHorarioEstado(int id, [FromBody] UpdateScheduleHourStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.UpdateHorarioEstadoAsync(id, request.Activo, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("horarios/{id:int}/eliminacion-preview")]
    public async Task<IActionResult> GetDeletionPreview(int id, CancellationToken cancellationToken)
    {
        var count = await schedulesService.GetHorarioDeletionPreviewAsync(id, cancellationToken);
        return Ok(new DeleteScheduleHourPreviewResponse { SlotsFuturos = count });
    }

    [HttpDelete("horarios/{id:int}")]
    public async Task<IActionResult> DeleteHorario(int id, [FromBody] DeleteScheduleHourRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.DeleteHorarioAsync(id, cancellationToken);
        return Ok(new OkResponse { Ok = result.Ok });
    }
}
