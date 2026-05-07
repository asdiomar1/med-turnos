using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Application.Features.Patients;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Patients;

public sealed class PatientsServiceTests
{
    private static readonly Guid ActorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ExistingPatientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MissingPatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid LinkedUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly IPatientRepository patientRepository = Substitute.For<IPatientRepository>();
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly IAdminEventFeedRepository adminEventFeedRepository = Substitute.For<IAdminEventFeedRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private PatientsService Sut => new(
        patientRepository,
        userRepository,
        adminEventFeedRepository,
        unitOfWork);

    [Fact]
    public async Task GetAsync_WhenRepositoryReturnsPatients_ReturnsMappedSummaries()
    {
        // Arrange
        var patientA = BuildPatient(id: ExistingPatientId, nombre: "Paciente Uno", telefono: "+541111111111", documentoIdentidad: "12345678");
        var patientB = BuildPatient(id: Guid.Parse("55555555-5555-5555-5555-555555555555"), nombre: "Paciente Dos", telefono: "+541122223333", documentoIdentidad: "AB1234", nacionalidad: "Uruguaya", loginIdentifier: null);

        patientRepository.GetAsync("paciente", false, Arg.Any<CancellationToken>()).Returns([patientA, patientB]);

        // Act
        var result = await Sut.GetAsync("paciente", includeInactive: false, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == ExistingPatientId && x.Nombre == "Paciente Uno" && x.Telefono == "+541111111111");
        Assert.Contains(result, x => x.Nombre == "Paciente Dos" && x.DocumentoIdentidad == "AB1234" && x.Nacionalidad == "Uruguaya");
        await patientRepository.Received(1).GetAsync("paciente", false, Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        patientRepository.GetAsync(null, true, Arg.Any<CancellationToken>()).Returns(Array.Empty<Patient>());

        // Act
        var result = await Sut.GetAsync(null, includeInactive: true, CancellationToken.None);

        // Assert
        Assert.Empty(result);
        await patientRepository.Received(1).GetAsync(null, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenValidCommand_PersistsPatientEmitsEventAndReturnsCreatedResult()
    {
        // Arrange
        var command = BuildCreateCommand();
        patientRepository.GetByLoginIdentifierAsync("paciente.login", Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        var result = await Sut.CreateAsync(ActorUserId, command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Paciente Nuevo", result.Nombre);
        await patientRepository.Received(1).GetByLoginIdentifierAsync("paciente.login", Arg.Any<CancellationToken>());
        await patientRepository.Received(1).AddAsync(
            Arg.Is<Patient>(patient =>
                patient.Nombre == "Paciente Nuevo" &&
                patient.Telefono == "+541112345678" &&
                patient.DocumentoIdentidad == "AB1234" &&
                patient.DocumentoIdentidadNormalizado == "ab1234" &&
                patient.LoginIdentifier == "paciente.login" &&
                patient.PortalHabilitado &&
                patient.OptInWhatsapp &&
                patient.OptInSource == "formulario"),
            Arg.Any<CancellationToken>());
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(entry =>
                entry.ActionCode == AdminEventFeedConstants.ActionCodes.PacienteCreated &&
                entry.ActionFamily == AdminEventFeedConstants.ActionFamilyPatient &&
                entry.EntityType == AdminEventFeedConstants.EntityTypes.Paciente &&
                entry.Summary == "Se creó el paciente \"Paciente Nuevo\"." &&
                entry.SourceSystem == AdminEventFeedConstants.SourceSystemApi &&
                entry.SourceRecordKey.StartsWith("paciente:paciente.created:")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenLoginIdentifierExists_ThrowsConflictExceptionAndDoesNotPersist()
    {
        // Arrange
        var command = BuildCreateCommand();
        patientRepository.GetByLoginIdentifierAsync("paciente.login", Arg.Any<CancellationToken>()).Returns(BuildPatient(loginIdentifier: "paciente.login"));

        // Act
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Sut.CreateAsync(ActorUserId, command, CancellationToken.None));

        // Assert
        Assert.Equal("login_identifier ya existe", ex.Message);
        await patientRepository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDatosExtraMalformed_ThrowsValidationException()
    {
        // Arrange
        var command = BuildCreateCommand(datosExtra: "{ not json");

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Sut.CreateAsync(ActorUserId, command, CancellationToken.None));

        // Assert
        Assert.Equal("datos_extra invalido", ex.Message);
        await patientRepository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDatosExtraIsNotObject_ThrowsValidationException()
    {
        // Arrange
        var command = BuildCreateCommand(datosExtra: "[]");

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Sut.CreateAsync(ActorUserId, command, CancellationToken.None));

        // Assert
        Assert.Equal("datos_extra invalido", ex.Message);
        await patientRepository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDocumentoHasLettersWithoutNationality_ThrowsValidationException()
    {
        // Arrange
        var command = BuildCreateCommand(documentoIdentidad: "AB1234", nacionalidad: null);

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Sut.CreateAsync(ActorUserId, command, CancellationToken.None));

        // Assert
        Assert.Equal("nacionalidad es obligatoria cuando el documento contiene letras", ex.Message);
        await patientRepository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenObraSocialWithoutCredential_ThrowsValidationException()
    {
        // Arrange
        var command = BuildCreateCommand(obraSocialId: 10, numeroCredencialObraSocial: null);

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Sut.CreateAsync(ActorUserId, command, CancellationToken.None));

        // Assert
        Assert.Equal("numero_credencial_obra_social es obligatorio si hay obra social", ex.Message);
        await patientRepository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenExistingPatient_UpdatesSummaryAndEmitsEvent()
    {
        // Arrange
        var patient = BuildPatient(
            id: ExistingPatientId,
            nombre: "Paciente Base",
            telefono: "+541100000000",
            documentoIdentidad: "12345678",
            nacionalidad: "Argentina",
            obraSocialId: 1,
            numeroCredencialObraSocial: "OLD-1",
            loginIdentifier: "paciente.base",
            notas: "Nota previa",
            datosExtra: "{}",
            optInWhatsapp: false,
            optInSource: null);

        patientRepository.GetByIdAsync(ExistingPatientId, Arg.Any<CancellationToken>()).Returns(patient);

        var command = BuildUpdateCommand();

        // Act
        var result = await Sut.UpdateAsync(ActorUserId, ExistingPatientId, command, CancellationToken.None);

        // Assert
        Assert.Equal(ExistingPatientId, result.Id);
        Assert.Equal("Paciente Base", result.Nombre);
        Assert.Equal("maria@example.com", result.Email);
        Assert.Equal("+541133334444", result.Telefono);
        Assert.Equal("87654321", result.DocumentoIdentidad);
        Assert.Equal("Argentina", result.Nacionalidad);
        Assert.Equal(2, result.CondicionIvaId);
        Assert.Equal(2, result.ObraSocialId);
        Assert.Equal("NEW-222", result.NumeroCredencialObraSocial);
        Assert.True(result.Claustrofobico);
        Assert.Equal("Nota actualizada", result.Notas);
        Assert.Equal("{\"fuente\":\"web\"}", result.DatosExtra);
        Assert.True(result.OptInWhatsapp);
        Assert.Equal("portal", result.OptInSource);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(entry =>
                entry.ActionCode == AdminEventFeedConstants.ActionCodes.PacienteUpdated &&
                entry.EntityType == AdminEventFeedConstants.EntityTypes.Paciente &&
                entry.Summary == "Se actualizaron los datos del paciente \"Paciente Base\"."),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenPatientNotFound_ThrowsNotFoundExceptionAndDoesNotPersist()
    {
        // Arrange
        patientRepository.GetByIdAsync(MissingPatientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => Sut.UpdateAsync(ActorUserId, MissingPatientId, BuildUpdateCommand(), CancellationToken.None));

        // Assert
        Assert.Equal("Paciente no encontrado", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenDatosExtraMalformed_ThrowsValidationException()
    {
        // Arrange
        var patient = BuildPatient(id: ExistingPatientId);
        patientRepository.GetByIdAsync(ExistingPatientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.UpdateAsync(ActorUserId, ExistingPatientId, BuildUpdateCommand(datosExtra: "{ broken json"), CancellationToken.None));

        // Assert
        Assert.Equal("datos_extra invalido", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenDatosExtraIsNotObject_ThrowsValidationException()
    {
        // Arrange
        var patient = BuildPatient(id: ExistingPatientId);
        patientRepository.GetByIdAsync(ExistingPatientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.UpdateAsync(ActorUserId, ExistingPatientId, BuildUpdateCommand(datosExtra: "[]"), CancellationToken.None));

        // Assert
        Assert.Equal("datos_extra invalido", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenPatientExists_DeactivatesAndSavesOnce()
    {
        // Arrange
        var patient = BuildPatient(id: ExistingPatientId);
        patientRepository.GetByIdAsync(ExistingPatientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        var result = await Sut.DeleteAsync(ExistingPatientId, CancellationToken.None);

        // Assert
        Assert.True(result.Ok);
        Assert.False(patient.IsActive);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenPatientMissing_ThrowsNotFoundException()
    {
        // Arrange
        patientRepository.GetByIdAsync(MissingPatientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => Sut.DeleteAsync(MissingPatientId, CancellationToken.None));

        // Assert
        Assert.Equal("Paciente no encontrado", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfigurePortalAsync_WhenEnablingPortalWithValidDocument_PersistsChangesAndReturnsSummary()
    {
        // Arrange
        var patient = BuildPatient(id: ExistingPatientId, documentoIdentidad: "AB1234", portalHabilitado: false);
        SetupPatientLookup(ExistingPatientId, patient);

        // Act
        var result = await Sut.ConfigurePortalAsync(ExistingPatientId, true, CancellationToken.None);

        // Assert
        Assert.Equal(ExistingPatientId, result.Id);
        Assert.True(result.PortalHabilitado);
        Assert.True(result.RequiereResetPortal);
        Assert.True(patient.PortalHabilitado);
        Assert.True(patient.RequiereResetPortal);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfigurePortalAsync_WhenEnablingPortalWithoutNormalizedDocument_ThrowsValidationException()
    {
        // Arrange
        SetupPatientLookup(
            ExistingPatientId,
            BuildPatient(id: ExistingPatientId, documentoIdentidad: "   ", portalHabilitado: false));

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Sut.ConfigurePortalAsync(ExistingPatientId, true, CancellationToken.None));

        // Assert
        Assert.Equal("Habilitar portal exige documento valido", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfigurePortalAsync_WhenPatientMissing_ThrowsNotFoundExceptionAndDoesNotSave()
    {
        // Arrange
        SetupPatientLookup(MissingPatientId, null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => Sut.ConfigurePortalAsync(MissingPatientId, true, CancellationToken.None));

        // Assert
        Assert.Equal("Paciente no encontrado", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnableResetAsync_WhenPatientExists_MarksResetAndSaves()
    {
        // Arrange
        var patient = BuildPatient(id: ExistingPatientId, portalHabilitado: true);
        SetupPatientLookup(ExistingPatientId, patient);

        // Act
        var result = await Sut.EnableResetAsync(ExistingPatientId, CancellationToken.None);

        // Assert
        Assert.Equal(ExistingPatientId, result.Id);
        Assert.True(result.RequiereResetPortal);
        Assert.True(patient.RequiereResetPortal);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnableResetAsync_WhenPatientMissing_ThrowsNotFoundExceptionAndDoesNotSave()
    {
        // Arrange
        SetupPatientLookup(MissingPatientId, null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => Sut.EnableResetAsync(MissingPatientId, CancellationToken.None));

        // Assert
        Assert.Equal("Paciente no encontrado", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMyDataAsync_WhenUserMissing_ThrowsUnauthorizedException()
    {
        // Arrange
        SetupUserLookup(ActorUserId, null);

        // Act
        await Assert.ThrowsAsync<UnauthorizedException>(() => Sut.UpdateMyDataAsync(ActorUserId, "Nuevo Nombre", "nuevo@example.com", "+541133334444", CancellationToken.None));

        // Assert
        await patientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMyDataAsync_WhenUserIsNotLinkedToPatient_ThrowsForbiddenException()
    {
        // Arrange
        SetupUserLookup(ActorUserId, BuildUser(ActorUserId, patientId: null));

        // Act
        await Assert.ThrowsAsync<ForbiddenException>(() => Sut.UpdateMyDataAsync(ActorUserId, "Nuevo Nombre", "nuevo@example.com", "+541133334444", CancellationToken.None));

        // Assert
        await patientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMyDataAsync_WhenLinkedPatientMissing_ThrowsNotFoundException()
    {
        // Arrange
        SetupUserLookup(ActorUserId, BuildUser(ActorUserId, patientId: ExistingPatientId));
        SetupPatientLookup(ExistingPatientId, null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => Sut.UpdateMyDataAsync(ActorUserId, "Nuevo Nombre", "nuevo@example.com", "+541133334444", CancellationToken.None));

        // Assert
        Assert.Equal("Paciente no encontrado", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "+541133334444")]
    [InlineData("Nuevo Nombre", "   ")]
    public async Task UpdateMyDataAsync_WhenNombreOrTelefonoMissing_ThrowsValidationException(string nombre, string telefono)
    {
        // Arrange
        SetupUserLookup(ActorUserId, BuildUser(ActorUserId, patientId: ExistingPatientId));
        SetupPatientLookup(ExistingPatientId, BuildPatient(id: ExistingPatientId));

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Sut.UpdateMyDataAsync(ActorUserId, nombre, "nuevo@example.com", telefono, CancellationToken.None));

        // Assert
        Assert.Equal("nombre y telefono son obligatorios", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMyDataAsync_WhenDataIsValid_UpdatesOwnProfileAndSaves()
    {
        // Arrange
        var patient = BuildPatient(id: ExistingPatientId, nombre: "Nombre Anterior", email: "viejo@example.com", telefono: "+541100000000");
        SetupUserLookup(ActorUserId, BuildUser(ActorUserId, patientId: ExistingPatientId));
        SetupPatientLookup(ExistingPatientId, patient);

        // Act
        var result = await Sut.UpdateMyDataAsync(ActorUserId, "  Nombre Nuevo  ", "  nuevo@example.com  ", "  +541133334444  ", CancellationToken.None);

        // Assert
        Assert.Equal(ExistingPatientId, result.Id);
        Assert.Equal("Nombre Nuevo", result.Nombre);
        Assert.Equal("nuevo@example.com", result.Email);
        Assert.Equal("+541133334444", result.Telefono);
        Assert.Equal("Nombre Nuevo", patient.Nombre);
        Assert.Equal("nuevo@example.com", patient.Email);
        Assert.Equal("+541133334444", patient.Telefono);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Patient BuildPatient(
        Guid? id = null,
        string nombre = "Paciente Test",
        string telefono = "+541112345678",
        string documentoIdentidad = "12345678",
        string? nacionalidad = "Argentina",
        int condicionIvaId = 1,
        int? obraSocialId = null,
        string? numeroCredencialObraSocial = null,
        string? loginIdentifier = "paciente.login",
        bool portalHabilitado = false,
        bool claustrofobico = false,
        string? notas = null,
        string datosExtra = "{}",
        bool optInWhatsapp = false,
        string? optInSource = null,
        string? email = null)
    {
        var patient = new Patient(
            id ?? Guid.NewGuid(),
            nombre,
            new PatientAdministrativeInfo(
                telefono,
                documentoIdentidad,
                new string(documentoIdentidad.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant(),
                condicionIvaId),
            new PatientPortalInfo(portalHabilitado, loginIdentifier));

        patient.UpdateAdministrativeData(new PatientAdministrativeDataUpdate(
            email,
            telefono,
            documentoIdentidad,
            new string(documentoIdentidad.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant(),
            nacionalidad,
            condicionIvaId,
            obraSocialId,
            numeroCredencialObraSocial,
            claustrofobico,
            notas,
            datosExtra,
            optInWhatsapp,
            optInSource));

        return patient;
    }

    private static CreatePatientCommand BuildCreateCommand(
        string nombre = "Paciente Nuevo",
        string? email = "paciente.nuevo@example.com",
        string telefono = "  +541112345678  ",
        string documentoIdentidad = " AB1234 ",
        string? loginIdentifier = "  Paciente.Login  ",
        string? nacionalidad = " Argentina ",
        int condicionIvaId = 1,
        int? obraSocialId = 10,
        string? numeroCredencialObraSocial = "  ABC-123  ",
        bool portalHabilitado = true,
        bool optInWhatsapp = true,
        string? optInSource = "formulario",
        bool claustrofobico = false,
        string? notas = "Observaciones",
        string datosExtra = "{\"origen\":\"web\"}")
        => new(
            nombre,
            email,
            telefono,
            documentoIdentidad,
            loginIdentifier,
            nacionalidad,
            condicionIvaId,
            obraSocialId,
            numeroCredencialObraSocial,
            portalHabilitado,
            optInWhatsapp,
            optInSource,
            claustrofobico,
            notas,
            datosExtra);

    private static UpdatePatientCommand BuildUpdateCommand(
        string? email = "maria@example.com",
        string telefono = "  +541133334444  ",
        string documentoIdentidad = " 87654321 ",
        string? nacionalidad = " Argentina ",
        int condicionIvaId = 2,
        int? obraSocialId = 2,
        string? numeroCredencialObraSocial = " NEW-222 ",
        bool claustrofobico = true,
        string? notas = " Nota actualizada ",
        string datosExtra = "{\"fuente\":\"web\"}",
        bool actualizarNotas = true,
        bool optInWhatsapp = true,
        string? optInSource = " portal ")
        => new(
            email,
            telefono,
            documentoIdentidad,
            nacionalidad,
            condicionIvaId,
            obraSocialId,
            numeroCredencialObraSocial,
            claustrofobico,
            notas,
            datosExtra,
            actualizarNotas,
            optInWhatsapp,
            optInSource);

    private static User BuildUser(Guid? id = null, Guid? patientId = null, string? nombre = "Usuario Test")
        => new(new UserCreateParams(
            id ?? Guid.NewGuid(),
            "usuario.test",
            "usuario.test@example.com",
            "hash",
            true,
            false,
            patientId,
            nombre));

    private void SetupPatientLookup(Guid patientId, Patient? patient)
    {
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
    }

    private void SetupUserLookup(Guid userId, User? user)
    {
        userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
    }
}
