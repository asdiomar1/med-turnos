using MedicalCenter.Api.Controllers.V1;
using MedicalCenter.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.IntegrationTests.Api.Controllers;

public sealed class PatientsControllerTests
{
    [Fact]
    public void MutationActions_WithPacienteId_HaveOwnershipFilter()
    {
        var methods = new[] { "Update", "Delete", "ConfigurePortal", "EnableReset" };
        foreach (var methodName in methods)
        {
            var method = typeof(PatientsController).GetMethod(methodName);
            Assert.NotNull(method);
            var hasFilter = method!.GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: false)
                .Cast<ServiceFilterAttribute>()
                .Any(a => a.ServiceType == typeof(OwnershipFilter));
            Assert.True(hasFilter, $"{methodName} should have OwnershipFilter");
        }
    }
}
