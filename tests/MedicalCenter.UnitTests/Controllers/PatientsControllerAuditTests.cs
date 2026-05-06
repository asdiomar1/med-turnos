using MedicalCenter.Api.Controllers.V1;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.Patients;
using MedicalCenter.Contracts.Patients;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedicalCenter.UnitTests.Controllers;

public sealed class PatientsControllerAuditTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();

    [Fact]
    public async Task Create_LogsDataMutation()
    {
        var service = new FakePatientsService();
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = CreateController(service, auditLogger);

        var result = await controller.Create(new CreatePatientRequest
        {
            Nombre = "Test",
            Telefono = "1234567890",
            DocumentoIdentidad = "12345678",
            CondicionIvaId = 1
        }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Single(auditLogger.Events);
        Assert.Equal("data_mutation", auditLogger.Events[0].EventType);
        Assert.Contains("create_patient", auditLogger.Events[0].Message);
    }

    [Fact]
    public async Task Update_LogsDataMutation()
    {
        var service = new FakePatientsService();
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = CreateController(service, auditLogger);

        var pacienteId = Guid.NewGuid();
        var result = await controller.Update(pacienteId, new UpdatePatientRequest
        {
            Telefono = "1234567890",
            DocumentoIdentidad = "12345678",
            CondicionIvaId = 1
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(auditLogger.Events);
        Assert.Equal("data_mutation", auditLogger.Events[0].EventType);
        Assert.Contains("update_patient", auditLogger.Events[0].Message);
    }

    [Fact]
    public async Task Delete_LogsDataMutation()
    {
        var service = new FakePatientsService();
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = CreateController(service, auditLogger);

        var pacienteId = Guid.NewGuid();
        var result = await controller.Delete(pacienteId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(auditLogger.Events);
        Assert.Equal("data_mutation", auditLogger.Events[0].EventType);
        Assert.Contains("delete_patient", auditLogger.Events[0].Message);
    }

    private static PatientsController CreateController(IPatientsService service, ISecurityAuditLogger auditLogger)
    {
        var controller = new PatientsController(service, auditLogger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()) },
                    "Bearer"))
            }
        };
        return controller;
    }

    private sealed class FakePatientsService : IPatientsService
    {
        public Task<IReadOnlyCollection<PatientSummary>> GetAsync(string? search, bool includeInactive, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<PatientSummary>>([]);

        public Task<CreatedPatientResult> CreateAsync(Guid actorUserId, CreatePatientCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new CreatedPatientResult(Guid.NewGuid(), command.Nombre));

        public Task<PatientSummary> UpdateAsync(Guid actorUserId, Guid patientId, UpdatePatientCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new PatientSummary(patientId, "Test", null, command.Telefono, command.DocumentoIdentidad, null, null, command.CondicionIvaId, command.ObraSocialId, null, false, false, null, false, null, "{}", false, null));

        public Task<MutationResult> DeleteAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult(new MutationResult(true));

        public Task<PatientSummary> ConfigurePortalAsync(Guid patientId, bool portalHabilitado, CancellationToken cancellationToken) =>
            Task.FromResult(new PatientSummary(patientId, "Test", null, "123", "doc", null, null, 1, null, null, false, false, null, false, null, "{}", false, null));

        public Task<PatientSummary> EnableResetAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult(new PatientSummary(patientId, "Test", null, "123", "doc", null, null, 1, null, null, false, false, null, false, null, "{}", false, null));

        public Task<PatientSummary> UpdateMyDataAsync(Guid userId, string nombre, string? email, string telefono, CancellationToken cancellationToken) =>
            Task.FromResult(new PatientSummary(userId, nombre, email, telefono, "doc", null, null, 1, null, null, false, false, null, false, null, "{}", false, null));
    }

    private sealed class FakeSecurityAuditLogger : ISecurityAuditLogger
    {
        public List<SecurityEvent> Events { get; } = [];
        public void LogAsync(SecurityEvent securityEvent) => Events.Add(securityEvent);
    }
}
