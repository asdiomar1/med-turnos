using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Application.Features.Catalogs;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Catalogs;

public sealed class CatalogsServiceTests
{
    private readonly ICondicionIvaRepository condicionIvaRepository = Substitute.For<ICondicionIvaRepository>();
    private readonly IObraSocialRepository obraSocialRepository = Substitute.For<IObraSocialRepository>();
    private readonly IAdminEventFeedRepository adminEventFeedRepository = Substitute.For<IAdminEventFeedRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid ActorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private CatalogsService Sut => new(
        condicionIvaRepository,
        obraSocialRepository,
        adminEventFeedRepository,
        unitOfWork);

    private static CondicionIva CreateCondicionIva(int id = 1, string nombre = "Responsable Inscripto", bool activo = true, int orden = 1)
        => new(id, nombre, activo, orden);

    private static ObraSocial CreateObraSocial(int id = 1, string nombre = "OSDE", bool activa = true, bool tieneConvenio = true, int orden = 0, string? abreviatura = "OSDE")
        => new(id, nombre, activa, tieneConvenio, orden, abreviatura);

    #region CondicionIva

    [Fact]
    public async Task GetCondicionesIvaAsync_WhenIncludeInactiveFalse_ReturnsOnlyActive()
    {
        condicionIvaRepository.GetAllAsync(false, Arg.Any<CancellationToken>()).Returns(new[]
        {
            CreateCondicionIva(1, "RI", true, 1),
            CreateCondicionIva(2, "Exento", true, 2)
        });

        var result = await Sut.GetCondicionesIvaAsync(includeInactive: false, CancellationToken.None);

        Assert.Equal(2, result.Count);
        await condicionIvaRepository.Received(1).GetAllAsync(false, Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCondicionesIvaAsync_WhenIncludeInactiveTrue_ReturnsAll()
    {
        condicionIvaRepository.GetAllAsync(true, Arg.Any<CancellationToken>()).Returns(new[]
        {
            CreateCondicionIva(1, "RI", true, 1),
            CreateCondicionIva(2, "Exento", false, 2),
            CreateCondicionIva(3, "Monotributo", true, 3)
        });

        var result = await Sut.GetCondicionesIvaAsync(includeInactive: true, CancellationToken.None);

        Assert.Equal(3, result.Count);
        await condicionIvaRepository.Received(1).GetAllAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCondicionIvaAsync_WithValidName_PersistsAndSavesTwice()
    {
        condicionIvaRepository.GetByNormalizedNameAsync("monotributo", null, Arg.Any<CancellationToken>())
            .Returns((CondicionIva?)null);
        condicionIvaRepository.GetNextOrderAsync(Arg.Any<CancellationToken>()).Returns(3);

        var result = await Sut.CreateCondicionIvaAsync(ActorUserId, "  monotributo  ", CancellationToken.None);

        Assert.Equal("monotributo", result.Nombre);
        await condicionIvaRepository.Received(1).AddAsync(
            Arg.Is<CondicionIva>(e => e.Nombre == "monotributo" && e.Activo == true && e.Orden == 3),
            Arg.Any<CancellationToken>());
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.CondicionIvaCreated &&
                e.EntityType == AdminEventFeedConstants.EntityTypes.CondicionIva &&
                e.SourceRecordKey.StartsWith("condicion_iva:condicion_iva.created:")),
            Arg.Any<CancellationToken>());
        // Create flow saves twice: entity then event-feed
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCondicionIvaAsync_WithEmptyName_ThrowsValidationException()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.CreateCondicionIvaAsync(ActorUserId, "   ", CancellationToken.None));

        Assert.Equal("El nombre es requerido.", ex.Message);
        await condicionIvaRepository.DidNotReceive().GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await condicionIvaRepository.DidNotReceive().AddAsync(Arg.Any<CondicionIva>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCondicionIvaAsync_WhenDuplicateExists_ThrowsConflictException()
    {
        condicionIvaRepository.GetByNormalizedNameAsync("Responsable Inscripto", null, Arg.Any<CancellationToken>())
            .Returns(CreateCondicionIva());

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Sut.CreateCondicionIvaAsync(ActorUserId, "Responsable Inscripto", CancellationToken.None));

        Assert.Equal("Ya existe una condición IVA con ese nombre.", ex.Message);
        await condicionIvaRepository.DidNotReceive().AddAsync(Arg.Any<CondicionIva>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCondicionIvaAsync_OnSuccess_CallsGetNextOrder()
    {
        condicionIvaRepository.GetByNormalizedNameAsync("monotributo", null, Arg.Any<CancellationToken>())
            .Returns((CondicionIva?)null);
        condicionIvaRepository.GetNextOrderAsync(Arg.Any<CancellationToken>()).Returns(3);

        await Sut.CreateCondicionIvaAsync(ActorUserId, "monotributo", CancellationToken.None);

        await condicionIvaRepository.Received(1).GetNextOrderAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCondicionIvaAsync_WithValidData_UpdatesEntityAndInvalidatesCache()
    {
        condicionIvaRepository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(CreateCondicionIva(id: 5, nombre: "Exento", orden: 1));
        condicionIvaRepository.GetByNormalizedNameAsync("Exento Actualizado", 5, Arg.Any<CancellationToken>())
            .Returns((CondicionIva?)null);

        var result = await Sut.UpdateCondicionIvaAsync(ActorUserId, 5, "Exento Actualizado", 2, CancellationToken.None);

        Assert.Equal("Exento Actualizado", result.Nombre);
        Assert.Equal(2, result.Orden);
        await condicionIvaRepository.Received(1).GetByIdAsync(5, Arg.Any<CancellationToken>());
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.CondicionIvaUpdated &&
                e.EntityType == AdminEventFeedConstants.EntityTypes.CondicionIva),
            Arg.Any<CancellationToken>());
        // exactly ONE call
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await condicionIvaRepository.Received(1).InvalidateCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCondicionIvaAsync_WhenIdNotFound_ThrowsNotFoundException()
    {
        condicionIvaRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((CondicionIva?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateCondicionIvaAsync(ActorUserId, 99, "Cualquier Nombre", 1, CancellationToken.None));

        Assert.Equal("Condición IVA no encontrada.", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCondicionIvaAsync_WithEmptyName_ThrowsValidationException()
    {
        condicionIvaRepository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(CreateCondicionIva(id: 5));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.UpdateCondicionIvaAsync(ActorUserId, 5, "", 1, CancellationToken.None));

        Assert.Equal("El nombre es requerido.", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCondicionIvaAsync_OnSuccess_InvalidatesCacheExactlyOnce()
    {
        condicionIvaRepository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(CreateCondicionIva(id: 5, nombre: "Exento", orden: 1));
        condicionIvaRepository.GetByNormalizedNameAsync("Exento Actualizado", 5, Arg.Any<CancellationToken>())
            .Returns((CondicionIva?)null);

        await Sut.UpdateCondicionIvaAsync(ActorUserId, 5, "Exento Actualizado", 2, CancellationToken.None);

        await condicionIvaRepository.Received(1).InvalidateCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCondicionIvaActiveAsync_WhenActivating_UpdatesFlagAndInvalidatesCache()
    {
        condicionIvaRepository.GetByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(CreateCondicionIva(id: 3, nombre: "Monotributo", activo: false));

        var result = await Sut.SetCondicionIvaActiveAsync(ActorUserId, 3, activo: true, CancellationToken.None);

        Assert.True(result.Activo);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.CondicionIvaStatusUpdated &&
                e.Summary.Contains("activa")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await condicionIvaRepository.Received(1).InvalidateCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCondicionIvaActiveAsync_WhenDeactivating_UpdatesFlagAndInvalidatesCache()
    {
        condicionIvaRepository.GetByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(CreateCondicionIva(id: 3, nombre: "Monotributo", activo: true));

        var result = await Sut.SetCondicionIvaActiveAsync(ActorUserId, 3, activo: false, CancellationToken.None);

        Assert.False(result.Activo);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.CondicionIvaStatusUpdated &&
                e.Summary.Contains("inactiva")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await condicionIvaRepository.Received(1).InvalidateCacheAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCondicionIvaActiveAsync_WhenIdNotFound_ThrowsNotFoundException()
    {
        condicionIvaRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((CondicionIva?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.SetCondicionIvaActiveAsync(ActorUserId, 99, activo: true, CancellationToken.None));

        Assert.Equal("Condición IVA no encontrada.", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await condicionIvaRepository.DidNotReceive().InvalidateCacheAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region ObraSocial

    [Fact]
    public async Task GetObrasSocialesAsync_ReturnsAllMappedToSummary()
    {
        obraSocialRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            CreateObraSocial(1),
            CreateObraSocial(2),
            CreateObraSocial(3),
            CreateObraSocial(4)
        });

        var result = await Sut.GetObrasSocialesAsync(CancellationToken.None);

        Assert.Equal(4, result.Count);
        await obraSocialRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateObraSocialAsync_WithValidData_PersistsAndSavesTwice()
    {
        obraSocialRepository.GetByNormalizedNameAsync("OSDE", null, Arg.Any<CancellationToken>())
            .Returns((ObraSocial?)null);

        var result = await Sut.CreateObraSocialAsync(ActorUserId, "OSDE", tieneConvenio: true, "osde", CancellationToken.None);

        Assert.Equal("OSDE", result.Nombre);
        Assert.True(result.TieneConvenio);
        Assert.Equal("OSDE", result.Abreviatura);
        await obraSocialRepository.Received(1).AddAsync(
            Arg.Is<ObraSocial>(e => e.Nombre == "OSDE" && e.TieneConvenio == true && e.Abreviatura == "OSDE" && e.Activa == true),
            Arg.Any<CancellationToken>());
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.ObraSocialCreated &&
                e.EntityType == AdminEventFeedConstants.EntityTypes.ObraSocial &&
                e.SourceRecordKey.StartsWith("obra_social:obra_social.created:")),
            Arg.Any<CancellationToken>());
        // Create flow saves twice: entity then event-feed
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateObraSocialAsync_WithNullAbreviatura_PersistsWithNullAbreviatura()
    {
        obraSocialRepository.GetByNormalizedNameAsync("Swiss Medical", null, Arg.Any<CancellationToken>())
            .Returns((ObraSocial?)null);

        var result = await Sut.CreateObraSocialAsync(ActorUserId, "Swiss Medical", false, null, CancellationToken.None);

        await obraSocialRepository.Received(1).AddAsync(
            Arg.Is<ObraSocial>(e => e.Abreviatura == null),
            Arg.Any<CancellationToken>());
        Assert.Null(result.Abreviatura);
        // Create flow saves twice: entity then event-feed
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateObraSocialAsync_WithEmptyName_ThrowsValidationException()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.CreateObraSocialAsync(ActorUserId, "", false, null, CancellationToken.None));

        Assert.Equal("El nombre es requerido.", ex.Message);
        await obraSocialRepository.DidNotReceive().AddAsync(Arg.Any<ObraSocial>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateObraSocialAsync_WhenDuplicateExists_ThrowsConflictException()
    {
        obraSocialRepository.GetByNormalizedNameAsync("OSDE", null, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial());

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Sut.CreateObraSocialAsync(ActorUserId, "OSDE", false, null, CancellationToken.None));

        Assert.Equal("Ya existe una obra social con ese nombre.", ex.Message);
        await obraSocialRepository.DidNotReceive().AddAsync(Arg.Any<ObraSocial>(), Arg.Any<CancellationToken>());
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateObraSocialAsync_WithTieneConvenioTrue_PersistsWithTieneConvenioTrue()
    {
        obraSocialRepository.GetByNormalizedNameAsync("Medife", null, Arg.Any<CancellationToken>())
            .Returns((ObraSocial?)null);

        var result = await Sut.CreateObraSocialAsync(ActorUserId, "Medife", true, null, CancellationToken.None);

        await obraSocialRepository.Received(1).AddAsync(
            Arg.Is<ObraSocial>(e => e.TieneConvenio == true),
            Arg.Any<CancellationToken>());
        Assert.True(result.TieneConvenio);
    }

    [Fact]
    public async Task UpdateObraSocialAsync_WithValidData_UpdatesEntityAndRegistersEvent()
    {
        obraSocialRepository.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial(id: 7, nombre: "OSDE", tieneConvenio: false));
        obraSocialRepository.GetByNormalizedNameAsync("OSDE Plus", 7, Arg.Any<CancellationToken>())
            .Returns((ObraSocial?)null);

        var result = await Sut.UpdateObraSocialAsync(ActorUserId, 7, "OSDE Plus", true, "OP", CancellationToken.None);

        Assert.Equal("OSDE Plus", result.Nombre);
        Assert.True(result.TieneConvenio);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.ObraSocialUpdated &&
                e.EntityType == AdminEventFeedConstants.EntityTypes.ObraSocial),
            Arg.Any<CancellationToken>());
        // exactly ONE call
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateObraSocialAsync_WhenIdNotFound_ThrowsNotFoundException()
    {
        obraSocialRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((ObraSocial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateObraSocialAsync(ActorUserId, 99, "Cualquier", false, null, CancellationToken.None));

        Assert.Equal("Obra social no encontrada.", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateObraSocialAsync_WithEmptyName_ThrowsValidationException()
    {
        obraSocialRepository.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial(id: 7));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut.UpdateObraSocialAsync(ActorUserId, 7, "  ", false, null, CancellationToken.None));

        Assert.Equal("El nombre es requerido.", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateObraSocialAsync_TogglingTieneConvenio_UpdatesField()
    {
        obraSocialRepository.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial(id: 7, nombre: "OSDE", tieneConvenio: false));
        obraSocialRepository.GetByNormalizedNameAsync("OSDE", 7, Arg.Any<CancellationToken>())
            .Returns((ObraSocial?)null);

        var result = await Sut.UpdateObraSocialAsync(ActorUserId, 7, "OSDE", tieneConvenio: true, null, CancellationToken.None);

        Assert.True(result.TieneConvenio);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetObraSocialActiveAsync_WhenActivating_UpdatesFlagAndRegistersEvent()
    {
        obraSocialRepository.GetByIdAsync(4, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial(id: 4, nombre: "Swiss Medical", activa: false));

        var result = await Sut.SetObraSocialActiveAsync(ActorUserId, 4, activa: true, CancellationToken.None);

        Assert.True(result.Activa);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.ObraSocialStatusUpdated &&
                e.Summary.Contains("activa")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetObraSocialActiveAsync_WhenDeactivating_UpdatesFlagAndRegistersEvent()
    {
        obraSocialRepository.GetByIdAsync(4, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial(id: 4, nombre: "Swiss Medical", activa: true));

        var result = await Sut.SetObraSocialActiveAsync(ActorUserId, 4, activa: false, CancellationToken.None);

        Assert.False(result.Activa);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.ObraSocialStatusUpdated &&
                e.Summary.Contains("inactiva")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetObraSocialActiveAsync_WhenIdNotFound_ThrowsNotFoundException()
    {
        obraSocialRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((ObraSocial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.SetObraSocialActiveAsync(ActorUserId, 99, activa: true, CancellationToken.None));

        Assert.Equal("Obra social no encontrada.", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetObraSocialConvenioAsync_WhenEnablingConvenio_UpdatesFlagAndRegistersEvent()
    {
        obraSocialRepository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial(id: 2, nombre: "Medife", tieneConvenio: false));

        var result = await Sut.SetObraSocialConvenioAsync(ActorUserId, 2, tieneConvenio: true, CancellationToken.None);

        Assert.True(result.TieneConvenio);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.ObraSocialConvenioUpdated &&
                e.Summary.Contains("sí")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetObraSocialConvenioAsync_WhenDisablingConvenio_UpdatesFlagAndRegistersEvent()
    {
        obraSocialRepository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(CreateObraSocial(id: 2, nombre: "Medife", tieneConvenio: true));

        var result = await Sut.SetObraSocialConvenioAsync(ActorUserId, 2, tieneConvenio: false, CancellationToken.None);

        Assert.False(result.TieneConvenio);
        await adminEventFeedRepository.Received(1).AddAsync(
            Arg.Is<AdminEventFeedEntry>(e =>
                e.ActionCode == AdminEventFeedConstants.ActionCodes.ObraSocialConvenioUpdated &&
                e.Summary.Contains("no")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetObraSocialConvenioAsync_WhenIdNotFound_ThrowsNotFoundException()
    {
        obraSocialRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((ObraSocial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.SetObraSocialConvenioAsync(ActorUserId, 99, tieneConvenio: true, CancellationToken.None));

        Assert.Equal("Obra social no encontrada.", ex.Message);
        await adminEventFeedRepository.DidNotReceive().AddAsync(Arg.Any<AdminEventFeedEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
