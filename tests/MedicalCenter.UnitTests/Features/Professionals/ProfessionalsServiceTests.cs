using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Application.Features.Professionals;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Professionals;

public sealed class ProfessionalsServiceTests
{
    private readonly IReferenteRepository referenteRepository = Substitute.For<IReferenteRepository>();
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly IAdminEventFeedRepository adminEventFeedRepository = Substitute.For<IAdminEventFeedRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid ActorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private ProfessionalsService Sut => new(referenteRepository, userRepository, adminEventFeedRepository, unitOfWork);

    private static Referente CreateReferente(
        string nombre = "Test Referente",
        string tipo = "doctor",
        bool activo = true,
        int orden = 1)
    {
        var r = new Referente(nombre, tipo, orden);
        if (!activo) r.SetActive(false);
        return r;
    }

    private static User CreateUser(
        Guid? id = null,
        string identifier = "user@test.com",
        string email = "user@test.com",
        string nombre = "Dr. Test",
        bool isActive = true)
        => new(new UserCreateParams(
            id ?? Guid.NewGuid(),
            identifier,
            email,
            "hash",
            isActive,
            true,
            Nombre: nombre));

    #region Medicos

    [Fact]
    public async Task GetMedicosAsync_WhenNoMedicos_ReturnsEmptyCollection()
    {
        userRepository.GetByRoleAsync("medico", true, Arg.Any<CancellationToken>()).Returns(Array.Empty<User>());

        var result = await Sut.GetMedicosAsync(CancellationToken.None);

        Assert.Empty(result);
        await userRepository.Received(1).GetByRoleAsync("medico", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMedicosAsync_WhenMedicosExist_MapsIdAndNombreToDto()
    {
        var user1 = CreateUser(identifier: "medico1@test.com", nombre: "Dr. House");
        var user2 = CreateUser(identifier: "medico2@test.com", nombre: "Dr. Grey");
        userRepository.GetByRoleAsync("medico", true, Arg.Any<CancellationToken>()).Returns(new[] { user1, user2 });

        var result = await Sut.GetMedicosAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Single(result, x => x.Id == user1.Id && x.Nombre == "Dr. House");
        Assert.Single(result, x => x.Id == user2.Id && x.Nombre == "Dr. Grey");
    }

    #endregion

    #region Referentes

    [Fact]
    public async Task GetReferentesAsync_WhenNoReferentes_ReturnsEmptyCollection()
    {
        referenteRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Referente>());

        var result = await Sut.GetReferentesAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReferentesAsync_WhenReferentesExist_MapsEntitiesToDto()
    {
        var r1 = CreateReferente(nombre: "Dr. Smith", tipo: "doctor");
        var r2 = CreateReferente(nombre: "Agencia Central", tipo: "agencia");
        referenteRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { r1, r2 });

        var result = await Sut.GetReferentesAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Single(result, x => x.Nombre == "Dr. Smith" && x.Tipo == "doctor");
        Assert.Single(result, x => x.Nombre == "Agencia Central" && x.Tipo == "agencia");
    }

    [Fact]
    public async Task CreateReferenteAsync_WithValidInput_PersistsAndSavesExactlyTwice()
    {
        referenteRepository.GetByNormalizedNameAndTypeAsync("Test Referente", "doctor", null, Arg.Any<CancellationToken>())
            .Returns((Referente?)null);
        referenteRepository.GetNextOrderAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await Sut.CreateReferenteAsync(ActorUserId, "Test Referente", "doctor", CancellationToken.None);

        Assert.Equal("Test Referente", result.Nombre);
        Assert.Equal("doctor", result.Tipo);
        await referenteRepository.Received(1).AddAsync(Arg.Any<Referente>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.Received(1).AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReferenteAsync_WithEmptyNombre_ThrowsValidationException()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.CreateReferenteAsync(ActorUserId, "   ", "doctor", CancellationToken.None));

        Assert.Equal("Nombre requerido.", ex.Message);
        await referenteRepository.DidNotReceive().AddAsync(Arg.Any<Referente>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReferenteAsync_WithUnknownTipo_ThrowsValidationException()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.CreateReferenteAsync(ActorUserId, "Test Referente", "unknown", CancellationToken.None));

        Assert.Equal("Tipo de referente inválido.", ex.Message);
        await referenteRepository.DidNotReceive().AddAsync(Arg.Any<Referente>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReferenteAsync_WhenDuplicateExists_ThrowsConflictException()
    {
        referenteRepository.GetByNormalizedNameAndTypeAsync("Test Referente", "doctor", null, Arg.Any<CancellationToken>())
            .Returns(CreateReferente());

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Sut.CreateReferenteAsync(ActorUserId, "Test Referente", "doctor", CancellationToken.None));

        Assert.Equal("Ya existe un referente con ese nombre y tipo.", ex.Message);
        await referenteRepository.DidNotReceive().AddAsync(Arg.Any<Referente>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReferenteAsync_WithInstitucionTipo_StoresAsAgencia()
    {
        referenteRepository.GetByNormalizedNameAndTypeAsync("Test", "agencia", null, Arg.Any<CancellationToken>())
            .Returns((Referente?)null);
        referenteRepository.GetNextOrderAsync(Arg.Any<CancellationToken>()).Returns(1);

        await Sut.CreateReferenteAsync(ActorUserId, "Test", "institucion", CancellationToken.None);

        await referenteRepository.Received(1).AddAsync(
            Arg.Is<Referente>(r => r.Tipo == "agencia"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReferenteAsync_WhenReferenteNotFound_ThrowsNotFoundException()
    {
        referenteRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Referente?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateReferenteAsync(ActorUserId, 999, "New Name", "doctor", CancellationToken.None));

        Assert.Equal("Referente no encontrado.", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReferenteAsync_WithEmptyNombre_ThrowsValidationException()
    {
        referenteRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(CreateReferente());

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.UpdateReferenteAsync(ActorUserId, 1, "   ", "doctor", CancellationToken.None));

        Assert.Equal("Nombre requerido.", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReferenteAsync_WhenDuplicateExcludingSelf_ThrowsConflictException()
    {
        var existingReferente = CreateReferente(nombre: "Test Referente", tipo: "doctor");
        referenteRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(existingReferente);
        referenteRepository.GetByNormalizedNameAndTypeAsync("New Name", "doctor", existingReferente.Id, Arg.Any<CancellationToken>())
            .Returns(CreateReferente(nombre: "New Name"));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Sut.UpdateReferenteAsync(ActorUserId, existingReferente.Id, "New Name", "doctor", CancellationToken.None));

        Assert.Equal("Ya existe un referente con ese nombre y tipo.", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReferenteAsync_WithValidInput_UpdatesEntityAndSavesExactlyOnce()
    {
        var existingReferente = CreateReferente(nombre: "Old Name", tipo: "doctor");
        referenteRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(existingReferente);
        referenteRepository.GetByNormalizedNameAndTypeAsync("New Name", "doctor", existingReferente.Id, Arg.Any<CancellationToken>())
            .Returns((Referente?)null);

        var result = await Sut.UpdateReferenteAsync(ActorUserId, existingReferente.Id, "New Name", "doctor", CancellationToken.None);

        Assert.Equal("New Name", result.Nombre);
        Assert.Equal("doctor", result.Tipo);
        await adminEventFeedRepository.Received(1).AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetReferenteActiveAsync_WhenReferenteNotFound_ThrowsNotFoundException()
    {
        referenteRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Referente?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.SetReferenteActiveAsync(ActorUserId, 999, false, CancellationToken.None));

        Assert.Equal("Referente no encontrado.", ex.Message);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetReferenteActiveAsync_WhenActive_DeactivatesAndSavesOnce()
    {
        referenteRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateReferente(activo: true));

        var result = await Sut.SetReferenteActiveAsync(ActorUserId, 1, false, CancellationToken.None);

        Assert.False(result.Activo);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetReferenteActiveAsync_WhenInactive_ActivatesAndSavesOnce()
    {
        referenteRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateReferente(activo: false));

        var result = await Sut.SetReferenteActiveAsync(ActorUserId, 1, true, CancellationToken.None);

        Assert.True(result.Activo);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Operadores

    [Fact]
    public async Task GetOperadoresAsync_WhenAllStaffInactive_ReturnsEmptyCollection()
    {
        userRepository.GetStaffAsync(false, Arg.Any<CancellationToken>()).Returns(new[]
        {
            CreateUser(identifier: "inactive1@test.com", isActive: false),
            CreateUser(identifier: "inactive2@test.com", isActive: false)
        });

        var result = await Sut.GetOperadoresAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOperadoresAsync_WhenMixedActiveAndInactive_ReturnsOnlyActiveEntries()
    {
        var activeUser = CreateUser(identifier: "active@test.com", nombre: "Active User", isActive: true);
        var inactiveUser = CreateUser(identifier: "inactive@test.com", nombre: "Inactive User", isActive: false);
        userRepository.GetStaffAsync(false, Arg.Any<CancellationToken>()).Returns(new[] { activeUser, inactiveUser });

        var result = await Sut.GetOperadoresAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(activeUser.Id, result.First().Id);
        Assert.DoesNotContain(result, x => x.Id == inactiveUser.Id);
    }

    #endregion
}
