using MedicalCenter.Api.Extensions;
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
        return Ok(items.Select(MapCamera));
    }

    [HttpGet("horarios")]
    public async Task<IActionResult> GetHorarios(CancellationToken cancellationToken)
    {
        var items = await schedulesService.GetHorariosAsync(cancellationToken);
        return Ok(items.Select(MapHour));
    }

    [HttpPost("camaras")]
    public async Task<IActionResult> CreateCamara([FromBody] CreateCameraRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.CreateCamaraAsync(User.GetUserId(), request.Nombre, request.Capacidad, cancellationToken);
        return Ok(MapCamera(result));
    }

    [HttpPatch("camaras/{id:int}")]
    public async Task<IActionResult> UpdateCamara(int id, [FromBody] UpdateCameraRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.UpdateCamaraAsync(User.GetUserId(), id, request.Nombre, request.Capacidad, cancellationToken);
        return Ok(new CameraMutationResponse
        {
            Camara = MapCamera(result.Camara),
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
        return Ok(MapCamera(result));
    }

    [HttpPost("horarios")]
    public async Task<IActionResult> CreateHorario([FromBody] CreateScheduleHourRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.CreateHorarioAsync(request.Hora, request.Orden, cancellationToken);
        return Ok(MapHour(result));
    }

    [HttpPatch("horarios/{id:int}")]
    public async Task<IActionResult> UpdateHorario(int id, [FromBody] UpdateScheduleHourRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.UpdateHorarioAsync(id, request.Hora, request.Orden, cancellationToken);
        return Ok(MapHour(result));
    }

    [HttpPatch("horarios/{id:int}/estado")]
    public async Task<IActionResult> UpdateHorarioEstado(int id, [FromBody] UpdateScheduleHourStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await schedulesService.UpdateHorarioEstadoAsync(id, request.Activo, cancellationToken);
        return Ok(MapHour(result));
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

    private static CameraResponse MapCamera(MedicalCenter.Application.DTOs.CameraSummary x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Capacidad = x.Capacidad,
        Activa = x.Activa
    };

    private static ScheduleHourResponse MapHour(MedicalCenter.Application.DTOs.ScheduleHourSummary x) => new()
    {
        Id = x.Id,
        Hora = x.Hora,
        Orden = x.Orden,
        Activo = x.Activo
    };
}
