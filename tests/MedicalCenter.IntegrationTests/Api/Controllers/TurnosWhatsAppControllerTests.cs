using MedicalCenter.Api.Controllers.V1;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Contracts.WhatsApp;
using MedicalCenter.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedicalCenter.IntegrationTests.Api.Controllers;

public sealed class TurnosWhatsAppControllerTests
{
    [Fact]
    public async Task Dispatch_WithApiKeyAuth_ReturnsOk()
    {
        var service = new FakeWhatsappService();
        var controller = new TurnosWhatsAppController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "service"),
                        new Claim("permission", "whatsapp.dispatch"),
                        new Claim("permission", "config.whatsapp.manage")
                    }, "ApiKey"))
            }
        };
        controller.HttpContext.Request.Headers["X-Api-Key"] = "valid-key";

        var result = await controller.Dispatch(new WhatsappDispatchRequest { SlotIds = new List<Guid>(), Limit = 10 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var attributes = typeof(TurnosWhatsAppController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: false);
        Assert.NotEmpty(attributes);
    }

    private sealed class FakeWhatsappService : IWhatsappService
    {
        public Task<WhatsappDispatchResult> DispatchAsync(WhatsappDispatchCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new WhatsappDispatchResult(0, 0));

        public Task<WhatsappReminderResult> SendRemindersAsync(WhatsappReminderCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new WhatsappReminderResult(DateOnly.FromDateTime(DateTime.Now), 0));

        public Task EnqueueTurnoConfirmadoAsync(Appointment appointment, string triggerSource, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task EnqueueTurnoCancelacionAsync(Appointment appointment, string triggerSource, string? operationKey, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task EnqueueTurnosCancelacionAsync(Guid patientId, IReadOnlyCollection<Appointment> appointments, string operationKey, string triggerSource, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
