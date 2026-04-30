using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.Features.Staff;
using MedicalCenter.Contracts.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/staff")]
[Authorize(Policy = "StaffManage")]
public sealed class StaffController(IStaffService staffService) : ControllerBase
{
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateMyStaffRequest request, CancellationToken cancellationToken)
    {
        var result = await staffService.UpdateMyDataAsync(User.GetUserId(), request.Nombre, cancellationToken);
        return Ok(new StaffProfileResponse
        {
            Id = result.Id,
            Identifier = result.Identifier,
            Email = result.Email ?? result.Identifier,
            Nombre = result.Nombre ?? result.Identifier,
            IsActive = result.IsActive,
            IsStaff = result.IsStaff,
            Roles = result.Roles
        });
    }
}
