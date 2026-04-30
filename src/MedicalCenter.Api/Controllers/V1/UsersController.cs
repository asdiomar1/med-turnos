using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
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
        return Ok(result.ToResponse());
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

        return Ok(result.ToResponse());
    }
}
