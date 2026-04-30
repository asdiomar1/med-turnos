using MedicalCenter.Api.Controllers.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedicalCenter.UnitTests.Controllers;

public sealed class PatientsControllerSecurityTests
{
    [Theory]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("ConfigurePortal")]
    [InlineData("EnableReset")]
    public void MutationAction_HasOwnershipFilter(string methodName)
    {
        var method = typeof(PatientsController).GetMethod(methodName)!;
        var attributes = method.GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: false);

        Assert.NotEmpty(attributes);
        var filterAttr = (ServiceFilterAttribute)attributes.First();
        Assert.Equal(typeof(MedicalCenter.Api.Filters.OwnershipFilter), filterAttr.ServiceType);
    }

    [Fact]
    public void UpdateMineAction_DoesNotHaveOwnershipFilter()
    {
        var method = typeof(PatientsController).GetMethod("UpdateMine")!;
        var attributes = method.GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: false);

        Assert.Empty(attributes);
    }
}
