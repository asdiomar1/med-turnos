using MedicalCenter.Api.Controllers.V1;
using Microsoft.AspNetCore.Authorization;

namespace MedicalCenter.UnitTests.Controllers;

public sealed class TurnosWhatsAppControllerTests
{
    [Fact]
    public void Controller_HasNoAllowAnonymousAttribute()
    {
        var attributes = typeof(TurnosWhatsAppController).GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false);
        Assert.Empty(attributes);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var attributes = typeof(TurnosWhatsAppController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);
        Assert.NotEmpty(attributes);
    }
}
