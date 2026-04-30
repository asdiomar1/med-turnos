using MedicalCenter.Api.Controllers;
using MedicalCenter.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.IntegrationTests.Api;

public sealed class HealthEndpointTests
{
    [Fact]
    public void GetHealth_ReturnsOkPayload()
    {
        var controller = new HealthController();

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<HealthResponse>(ok.Value);
        Assert.Equal("ok", payload.Status);
    }
}
