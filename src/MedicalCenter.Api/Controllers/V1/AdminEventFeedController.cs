using System.Text.Json;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Contracts.AdminEventFeed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/admin/event-feed")]
[Authorize(Policy = "ActivityRead")]
public sealed class AdminEventFeedController(IAdminEventFeedService adminEventFeedService) : ControllerBase
{
    [HttpGet("action-codes")]
    public IActionResult GetActionCodes()
    {
        var items = AdminEventFeedConstants.CatalogActionDefinitions
            .OrderBy(x => x.Label)
            .Select(x => new AdminEventActionCodeResponse
            {
                Code = x.Code,
                Family = x.Family,
                EntityType = x.EntityType,
                Label = x.Label
            })
            .ToArray();

        return Ok(items);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? limit,
        [FromQuery] DateTimeOffset? beforeOccurredAt,
        [FromQuery] long? beforeId,
        [FromQuery] Guid? actorUserId,
        [FromQuery(Name = "actionCodes")] string[] actionCodes,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var items = await adminEventFeedService.ListAsync(
            limit ?? 50,
            beforeOccurredAt,
            beforeId,
            actorUserId,
            actionCodes,
            dateFrom,
            dateTo,
            cancellationToken);

        return Ok(items.Select(Map));
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        var options = await adminEventFeedService.GetFilterOptionsAsync(cancellationToken);
        return Ok(Map(options));
    }

    private static AdminEventFeedItemResponse Map(MedicalCenter.Application.DTOs.AdminEventFeedItemDto x) => new()
    {
        Id = x.Id,
        OccurredAt = x.OccurredAt,
        ActorUserId = x.ActorUserId,
        ActorLabel = x.ActorLabel,
        ActionCode = x.ActionCode,
        ActionFamily = x.ActionFamily,
        EntityType = x.EntityType,
        EntityId = x.EntityId,
        AgendaType = x.AgendaType,
        PacienteId = x.PacienteId,
        PacienteNombre = x.PacienteNombre,
        MedicoId = x.MedicoId,
        MedicoNombre = x.MedicoNombre,
        Title = x.Title,
        Summary = x.Summary,
        Metadata = ParseMetadata(x.MetadataJson)
    };

    private static AdminEventFeedFilterOptionsResponse Map(MedicalCenter.Application.DTOs.AdminEventFeedFilterOptionsDto x) => new()
    {
        Actors = x.Actors.Select(actor => new AdminEventFeedActorOptionResponse
        {
            Id = actor.Id,
            Label = actor.Label
        }).ToArray(),
        Actions = x.Actions.Select(action => new AdminEventFeedActionOptionResponse
        {
            Code = action.Code,
            Family = action.Family,
            Label = action.Label
        }).ToArray()
    };

    private static JsonElement ParseMetadata(string metadataJson)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson);
            return document.RootElement.Clone();
        }
        catch
        {
            using var fallback = JsonDocument.Parse("{}");
            return fallback.RootElement.Clone();
        }
    }
}
