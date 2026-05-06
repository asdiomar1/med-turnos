using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.DTOs;
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
    [ProducesResponseType(typeof(AdminEventActionCodeResponse[]), StatusCodes.Status200OK)]
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
    public async Task<IActionResult> List([FromQuery] AdminEventFeedListQuery query, CancellationToken cancellationToken)
    {
        var items = await adminEventFeedService.ListAsync(new AdminEventFeedQuery(
            query.Limit ?? 50,
            query.BeforeOccurredAt,
            query.BeforeId,
            query.ActorUserId,
            query.ActionCodes,
            query.DateFrom,
            query.DateTo), cancellationToken);

        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        var options = await adminEventFeedService.GetFilterOptionsAsync(cancellationToken);
        return Ok(options.ToResponse());
    }
}

public sealed class AdminEventFeedListQuery
{
    public int? Limit { get; init; }
    public DateTimeOffset? BeforeOccurredAt { get; init; }
    public long? BeforeId { get; init; }
    public Guid? ActorUserId { get; init; }
    [FromQuery(Name = "actionCodes")]
    public string[] ActionCodes { get; init; } = [];
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}
