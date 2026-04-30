using System.Text.Json;
using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.Features.UserPreferences;
using MedicalCenter.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(IUserPreferencesService userPreferencesService) : ControllerBase
{
    [HttpGet("me/preferences")]
    public async Task<IActionResult> GetMyPreferences(CancellationToken cancellationToken)
    {
        var result = await userPreferencesService.GetAsync(User.GetUserId(), cancellationToken);
        return Ok(Map(result));
    }

    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpsertMyPreferences([FromBody] UpdateUserPreferencesRequest request, CancellationToken cancellationToken)
    {
        request ??= new UpdateUserPreferencesRequest();
        var result = await userPreferencesService.UpsertAsync(
            new MedicalCenter.Application.DTOs.UpdateUserPreferencesCommand(
                User.GetUserId(),
                request.Theme,
                request.CustomColors.HasValue ? request.CustomColors.Value.GetRawText() : null,
                request.TurnosLayout,
                request.FontScale),
            cancellationToken);

        return Ok(Map(result));
    }

    private static UserPreferencesResponse Map(MedicalCenter.Application.DTOs.UserPreferencesSummary x) => new()
    {
        UserId = x.UserId,
        Theme = x.Theme,
        CustomColors = ParseJson(x.CustomColorsJson),
        TurnosLayout = x.TurnosLayout,
        FontScale = x.FontScale,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
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
