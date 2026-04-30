using MedicalCenter.Api.Mappings;
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

        return Ok(items.Select(x => x.ToResponse()));
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        var options = await adminEventFeedService.GetFilterOptionsAsync(cancellationToken);
        return Ok(options.ToResponse());
    }
}
