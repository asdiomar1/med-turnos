using MedicalCenter.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new HealthResponse());
}
