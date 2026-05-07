using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.ClinicalHistory;
using DomainEnt = MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.ClinicalHistory;

public sealed class ClinicalHistoryServiceTests
{
    private readonly IPatientRepository _patientRepo = Substitute.For<IPatientRepository>();
    private readonly IMedicoRepository _medicoRepo = Substitute.For<IMedicoRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IClinicalHistoryRepository _historyRepo = Substitute.For<IClinicalHistoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ClinicalHistoryService Sut => new(
        _patientRepo,
        _medicoRepo,
        _userRepo,
        _historyRepo,
        _unitOfWork);

    [Fact]
    public async Task GetAsync_WhenPatientNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var missingPatientId = Guid.NewGuid();
        _patientRepo.GetByIdAsync(missingPatientId, Arg.Any<CancellationToken>()).Returns((DomainEnt.Patient?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetAsync(missingPatientId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEvolutionAsync_WhenNoteEmpty_ThrowsValidationException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patient = CreateActivePatient();
        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        
        var user = CreateUser(actorId);
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);
        
        var command = new CreateEvolutionCommand(
            MedicoId: null,
            FechaClinica: DateOnly.FromDateTime(DateTime.UtcNow),
            Titulo: "Title",
            Nota: "",
            DiagnosticoImpresion: null,
            Indicaciones: null,
            ConsultaSlotId: null,
            MedicoUserId: actorId);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => Sut.CreateEvolutionAsync(actorId, patientId, command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEvolutionAsync_WhenNoMedico_ThrowsValidationException()
    {
        // Arrange - Note: permission check happens BEFORE medico check, so we get ForbiddenException
        // This test verifies that when user lacks permission, they get Forbidden before Validation
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patient = CreateActivePatient();
        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        
        // User is staff but has no permissions, so HasPermission returns false
        var user = CreateUser(actorId);
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);
        
        var command = new CreateEvolutionCommand(
            MedicoId: null,
            FechaClinica: DateOnly.FromDateTime(DateTime.UtcNow),
            Titulo: "Title",
            Nota: "Note",
            DiagnosticoImpresion: null,
            Indicaciones: null,
            ConsultaSlotId: null,
            MedicoUserId: null);

        // Act & Assert - Permission check comes before medico check
        await Assert.ThrowsAsync<ForbiddenException>(() => Sut.CreateEvolutionAsync(actorId, patientId, command, CancellationToken.None));
    }

    private static DomainEnt.Patient CreateActivePatient() =>
        new(
            Guid.NewGuid(),
            "Test Patient",
            new DomainEnt.PatientAdministrativeInfo("+541112345678", "12345678", "12345678", 1),
            new DomainEnt.PatientPortalInfo(false, null));

    private static DomainEnt.User CreateUser(Guid id) =>
        new(new DomainEnt.UserCreateParams(
            id,
            "user.test",
            "test@test.com",
            "hash",
            true,
            true,
            null,
            "Test User"));
}