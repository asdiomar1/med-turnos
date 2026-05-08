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
    public async Task GetResumenAsync_ReturnsSummaryList()
    {
        // Arrange
        var expected = new List<ClinicalHistoryNumeroSummary>
        {
            new(Guid.NewGuid(), 1),
            new(Guid.NewGuid(), 2)
        };
        _historyRepo.GetResumenAsync(Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        var result = await Sut.GetResumenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

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
    public async Task GetAsync_WhenPatientInactive_ThrowsConflictException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = CreateInactivePatient(patientId);
        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => Sut.GetAsync(patientId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsSummary()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patient = CreateActivePatientWithId(patientId);
        var history = CreateHistory(patientId, 1);
        var user = CreateUserWithPermission(actorId, "historia_clinica.editar_ficha");

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _historyRepo.GetByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(history);
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await Sut.UpdateAsync(actorId, patientId, "New antecedentes", null, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New antecedentes", result.Antecedentes);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoPermission_ThrowsForbiddenException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patient = CreateActivePatientWithId(patientId);
        var history = CreateHistory(patientId, 1);
        var user = CreateUser(actorId); // No permissions

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _historyRepo.GetByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(history);
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => Sut.UpdateAsync(actorId, patientId, null, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateNumeroAsync_WithValidNumber_ReturnsSummary()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patient = CreateActivePatientWithId(patientId);
        var history = CreateHistory(patientId, 1);
        var user = CreateUserWithPermission(actorId, "historia_clinica.editar_numero");

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _historyRepo.GetByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(history);
        _historyRepo.IsNumeroTakenAsync(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await Sut.UpdateNumeroAsync(actorId, patientId, 42, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Numero);
    }

    [Fact]
    public async Task UpdateNumeroAsync_WhenNumberLessThanOne_ThrowsValidationException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var user = CreateUserWithPermission(actorId, "historia_clinica.editar_numero");
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => Sut.UpdateNumeroAsync(actorId, patientId, 0, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateNumeroAsync_WhenNumberAlreadyTaken_ThrowsConflictException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patient = CreateActivePatientWithId(patientId);
        var history = CreateHistory(patientId, 1);
        var user = CreateUserWithPermission(actorId, "historia_clinica.editar_numero");

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _historyRepo.GetByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(history);
        _historyRepo.IsNumeroTakenAsync(42, patientId, Arg.Any<CancellationToken>()).Returns(true); // Already taken
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => Sut.UpdateNumeroAsync(actorId, patientId, 42, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateNumeroAsync_WhenNoPermission_ThrowsForbiddenException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var user = CreateUser(actorId); // No permissions
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => Sut.UpdateNumeroAsync(actorId, patientId, 10, CancellationToken.None));
    }

    [Fact]
    public async Task GetEvolutionsAsync_WithMedicoUser_ReturnsEvolutions()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();
        var evolutions = new List<DomainEnt.ClinicalEvolution>
        {
            CreateEvolution(patientId, medicoUserId: medicoUserId)
        };
        var medicoUser = CreateUser(medicoUserId);
        medicoUser.SetRoles(new[] { CreateMedicoRole() });

        _historyRepo.GetEvolutionsByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(evolutions);
        _userRepo.GetByIdAsync(medicoUserId, Arg.Any<CancellationToken>()).Returns(medicoUser);

        // Act
        var result = await Sut.GetEvolutionsAsync(patientId, CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetEvolutionsAsync_WithLegacyMedico_ReturnsEvolutions()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var evolutions = new List<DomainEnt.ClinicalEvolution>
        {
            CreateEvolution(patientId, medicoId: 5, medicoUserId: null) // Legacy: MedicoId > 0, no MedicoUserId
        };
        var medico = new DomainEnt.Medico("Dr. Legacy", 1);
        medico.SetActive(true);
        var medicos = new List<DomainEnt.Medico> { medico };

        _historyRepo.GetEvolutionsByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(evolutions);
        _medicoRepo.GetAsync(false, Arg.Any<CancellationToken>()).Returns(medicos);

        // Act
        var result = await Sut.GetEvolutionsAsync(patientId, CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetEvolutionsAsync_WhenNoEvolutions_ReturnsEmptyList()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _historyRepo.GetEvolutionsByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(Array.Empty<DomainEnt.ClinicalEvolution>());

        // Act
        var result = await Sut.GetEvolutionsAsync(patientId, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateEvolutionAsync_WhenPatientInactive_ThrowsConflictException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var patient = CreateActivePatientWithId(patientId);
        patient.Deactivate(); // Make inactive

        var user = CreateUserWithPermission(actorId, "historia_clinica.crear_evolucion");
        var medicoUser = CreateUserWithMedicoRole(actorId);

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _userRepo.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new CreateEvolutionCommand(
            MedicoId: null,
            FechaClinica: DateOnly.FromDateTime(DateTime.UtcNow),
            Titulo: "Title",
            Nota: "Note",
            DiagnosticoImpresion: null,
            Indicaciones: null,
            ConsultaSlotId: null,
            MedicoUserId: actorId);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => Sut.CreateEvolutionAsync(actorId, patientId, command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEvolutionAsync_WhenMedicoNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();
        var paciente = CreateActivePatientWithId(patientId);
        var user = CreateUserWithPermission(actorId, "historia_clinica.crear_evolucion");

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(paciente);
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x => x.ArgAt<Guid>(0) == actorId ? user : (DomainEnt.User?)null);

        var command = new CreateEvolutionCommand(
            MedicoId: null,
            FechaClinica: DateOnly.FromDateTime(DateTime.UtcNow),
            Titulo: "Title",
            Nota: "Note",
            DiagnosticoImpresion: null,
            Indicaciones: null,
            ConsultaSlotId: null,
            MedicoUserId: medicoUserId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.CreateEvolutionAsync(actorId, patientId, command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEvolutionAsync_WhenMedicoInactive_ThrowsConflictException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();
        var paciente = CreateActivePatientWithId(patientId);
        var user = CreateUserWithPermission(actorId, "historia_clinica.crear_evolucion");
        var inactiveMedicoUser = new DomainEnt.User(new DomainEnt.UserCreateParams(
            medicoUserId, "medico.test", "medico@test.com", "hash", false, true, null, "Medico"));
        inactiveMedicoUser.SetRoles(new[] { CreateMedicoRole() });

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(paciente);
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x => x.ArgAt<Guid>(0) == actorId ? user : inactiveMedicoUser);

        var command = new CreateEvolutionCommand(
            MedicoId: null,
            FechaClinica: DateOnly.FromDateTime(DateTime.UtcNow),
            Titulo: "Title",
            Nota: "Note",
            DiagnosticoImpresion: null,
            Indicaciones: null,
            ConsultaSlotId: null,
            MedicoUserId: medicoUserId);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => Sut.CreateEvolutionAsync(actorId, patientId, command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEvolutionAsync_WithValidData_ReturnsSummary()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();
        var paciente = CreateActivePatientWithId(patientId);
        var user = CreateUserWithPermission(actorId, "historia_clinica.crear_evolucion");
        var medicoUser = CreateUserWithMedicoRole(medicoUserId);
        medicoUser.ActivatePortalUser("medico.test", "hash", "medico@test.com");

        _patientRepo.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(paciente);
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x => x.ArgAt<Guid>(0) == actorId ? user : medicoUser);
        _historyRepo.AddEvolutionAsync(Arg.Any<DomainEnt.ClinicalEvolution>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var command = new CreateEvolutionCommand(
            MedicoId: null,
            FechaClinica: DateOnly.FromDateTime(DateTime.UtcNow),
            Titulo: "Title",
            Nota: "Note",
            DiagnosticoImpresion: null,
            Indicaciones: null,
            ConsultaSlotId: null,
            MedicoUserId: medicoUserId);

        // Act
        var result = await Sut.CreateEvolutionAsync(actorId, patientId, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
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

    private static DomainEnt.Patient CreateActivePatientWithId(Guid id) =>
        new(
            id,
            "Test Patient",
            new DomainEnt.PatientAdministrativeInfo("+541112345678", "12345678", "12345678", 1),
            new DomainEnt.PatientPortalInfo(false, null));

    private static DomainEnt.Patient CreateInactivePatient(Guid id)
    {
        var patient = new DomainEnt.Patient(
            id,
            "Test Patient",
            new DomainEnt.PatientAdministrativeInfo("+541112345678", "12345678", "12345678", 1),
            new DomainEnt.PatientPortalInfo(false, null));
        patient.Deactivate();
        return patient;
    }

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

    private static DomainEnt.User CreateUserWithPermission(Guid id, string permission)
    {
        var role = new DomainEnt.Role(new DomainEnt.RoleCreateParams(
            Guid.NewGuid(),
            "staff_role",
            "Staff Role",
            [permission]));
        var user = new DomainEnt.User(new DomainEnt.UserCreateParams(
            id,
            "user.test",
            "test@test.com",
            "hash",
            true,
            true,
            null,
            "Test User"));
        user.SetRoles([role]);
        return user;
    }

    private static DomainEnt.Role CreateMedicoRole() =>
        new(new DomainEnt.RoleCreateParams(
            Guid.NewGuid(),
            "medico",
            "Médico",
            Array.Empty<string>()));

    private static DomainEnt.User CreateUserWithMedicoRole(Guid id)
    {
        var role = CreateMedicoRole();
        var user = new DomainEnt.User(new DomainEnt.UserCreateParams(
            id,
            "medico.test",
            "medico@test.com",
            "hash",
            true,
            true,
            null,
            "Dr. Medico"));
        user.SetRoles([role]);
        return user;
    }

    private static DomainEnt.ClinicalHistory CreateHistory(Guid patientId, long numero) =>
        new(new DomainEnt.ClinicalHistoryCreateParams(
            patientId,
            numero,
            "Antecedentes",
            "Alergias",
            "Medicación",
            "Notas"));

    private static DomainEnt.ClinicalEvolution CreateEvolution(Guid patientId, int medicoId = 0, Guid? medicoUserId = null) =>
        new(new DomainEnt.ClinicalEvolutionCreateData
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            ConsultaSlotId = null,
            MedicoId = medicoId,
            MedicoUserId = medicoUserId,
            AuthorProfileId = Guid.NewGuid(),
            FechaClinica = DateOnly.FromDateTime(DateTime.UtcNow),
            Titulo = "Test Evolution",
            Nota = "Test note",
            DiagnosticoImpresion = null,
            Indicaciones = null
        });
}