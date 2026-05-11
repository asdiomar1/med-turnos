using MedicalCenter.Domain.Entities;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments;

/// <summary>
/// Tests the dual-path medico resolution (MedicoUserId primary, MedicoId fallback)
/// in AppointmentsService enrichment methods.
/// </summary>
public sealed class MedicoServiceResolutionTests : IClassFixture<AppointmentsServiceTestFixture>
{
    private readonly AppointmentsServiceTestFixture _fixture;

    public MedicoServiceResolutionTests(AppointmentsServiceTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    #region Repository: GetByMedicoUserIdAsync

    [Fact]
    public async Task GetByMedicoUserIdAsync_WithNullGuid_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.CreateSut();

        // Act
        var result = await _fixture.MedicoRepository.GetByMedicoUserIdAsync(null, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByMedicoUserIdAsync_WithEmptyGuid_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.CreateSut();

        // Act
        var result = await _fixture.MedicoRepository.GetByMedicoUserIdAsync(Guid.Empty, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByDateAsync: BuildAppointmentSummaryAsync dual-path

    [Fact]
    public async Task GetByDateAsync_WithMedicoUserId_ResolvesMedicoByGuid()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var medicoUserId = Guid.NewGuid();
        var medico = new Medico("Dr. García", 1, medicoUserId);
        medico.SetIdGuid(Guid.NewGuid());
        var appointment = new AppointmentBuilder()
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(Guid.NewGuid())
            .WithMedicoUserId(medicoUserId)
            .Build();

        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());
        _fixture.MedicoRepository
            .GetByMedicoUserIdAsync(medicoUserId, Arg.Any<CancellationToken>())
            .Returns(medico);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByDateAsync(date, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.NotNull(summary.Medico);
        Assert.Equal(medico.Nombre, summary.Medico.Nombre);
        Assert.Equal(medico.Id, summary.Medico.Id);
        await _fixture.MedicoRepository
            .Received(1)
            .GetByMedicoUserIdAsync(medicoUserId, Arg.Any<CancellationToken>());
        await _fixture.MedicoRepository
            .DidNotReceiveWithAnyArgs()
            .GetByIdAsync(default, default);
    }

    [Fact]
    public async Task GetByDateAsync_WithOnlyMedicoId_FallsBackToIntLookup()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var medicoId = 42;
        var medico = new Medico("Dr. López", 2);
        typeof(Medico).GetProperty(nameof(Medico.Id))?.SetValue(medico, medicoId);
        medico.SetIdGuid(Guid.NewGuid());
        var appointment = new AppointmentBuilder()
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(Guid.NewGuid())
            .WithMedicoId(medicoId)
            .Build();

        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());
        _fixture.MedicoRepository
            .GetByIdAsync(medicoId, Arg.Any<CancellationToken>())
            .Returns(medico);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByDateAsync(date, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.NotNull(summary.Medico);
        Assert.Equal(medico.Nombre, summary.Medico.Nombre);
        Assert.Equal(medicoId, summary.Medico.Id);
        await _fixture.MedicoRepository
            .Received(1)
            .GetByIdAsync(medicoId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByDateAsync_WithBothIds_PrefersMedicoUserId()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var medicoUserId = Guid.NewGuid();
        var medicoId = 42;
        var medicoByUser = new Medico("Dr. Primario", 1, medicoUserId);
        medicoByUser.SetIdGuid(Guid.NewGuid());
        var appointment = new AppointmentBuilder()
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(Guid.NewGuid())
            .WithMedicoUserId(medicoUserId)
            .WithMedicoId(medicoId)
            .Build();

        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());
        _fixture.MedicoRepository
            .GetByMedicoUserIdAsync(medicoUserId, Arg.Any<CancellationToken>())
            .Returns(medicoByUser);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByDateAsync(date, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.NotNull(summary.Medico);
        Assert.Equal("Dr. Primario", summary.Medico.Nombre);
        await _fixture.MedicoRepository
            .Received(1)
            .GetByMedicoUserIdAsync(medicoUserId, Arg.Any<CancellationToken>());
        await _fixture.MedicoRepository
            .DidNotReceiveWithAnyArgs()
            .GetByIdAsync(default, default);
    }

    [Fact]
    public async Task GetByDateAsync_WithNoMedicoIds_ReturnsNullMedico()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var appointment = new AppointmentBuilder()
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(Guid.NewGuid())
            .Build();

        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByDateAsync(date, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.Null(summary.Medico);
    }

    #endregion

    #region GetBlockHistoryAsync: MapBlockHistoryAsync dual-path

    [Fact]
    public async Task GetBlockHistoryAsync_WithMedicoUserId_ResolvesMedicoByGuid()
    {
        // Arrange
        var fecha = new DateOnly(2026, 5, 15);
        var hora = new TimeOnly(9, 0);
        var medicoUserId = Guid.NewGuid();
        var medico = new Medico("Dr. Historia", 1, medicoUserId);
        medico.SetIdGuid(Guid.NewGuid());
        var history = new BlockHistory(new BlockHistoryCreateParams(
            Guid.NewGuid(), fecha, hora, 1, Guid.NewGuid(), 1, "test", null, null, null,
            false, "particular", null, null, null, null, null, medicoUserId, null,
            false, null, null, null, null));

        _fixture.BlockHistoryRepository
            .GetByBlockAsync(fecha, hora, null, Arg.Any<CancellationToken>())
            .Returns([history]);
        _fixture.MedicoRepository
            .GetByMedicoUserIdAsync(medicoUserId, Arg.Any<CancellationToken>())
            .Returns(medico);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetBlockHistoryAsync(fecha, hora, null, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.NotNull(summary.Medico);
        Assert.Equal(medico.Nombre, summary.Medico.Nombre);
        await _fixture.MedicoRepository
            .Received(1)
            .GetByMedicoUserIdAsync(medicoUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBlockHistoryAsync_WithOnlyMedicoId_FallsBackToIntLookup()
    {
        // Arrange
        var fecha = new DateOnly(2026, 5, 15);
        var hora = new TimeOnly(9, 0);
        var medicoId = 99;
        var medico = new Medico("Dr. Fallback", 2);
        typeof(Medico).GetProperty(nameof(Medico.Id))?.SetValue(medico, medicoId);
        medico.SetIdGuid(Guid.NewGuid());
        var history = new BlockHistory(new BlockHistoryCreateParams(
            Guid.NewGuid(), fecha, hora, 1, Guid.NewGuid(), 1, "test", null, null, null,
            false, "particular", null, null, null, null, medicoId, null, null,
            false, null, null, null, null));

        _fixture.BlockHistoryRepository
            .GetByBlockAsync(fecha, hora, null, Arg.Any<CancellationToken>())
            .Returns([history]);
        _fixture.MedicoRepository
            .GetByIdAsync(medicoId, Arg.Any<CancellationToken>())
            .Returns(medico);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetBlockHistoryAsync(fecha, hora, null, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.NotNull(summary.Medico);
        Assert.Equal(medico.Nombre, summary.Medico.Nombre);
        Assert.Equal(medicoId, summary.Medico.Id);
    }

    #endregion

    #region GetEnrichedByDateAsync: MapMedicoEnrichedSummary dual-path

    [Fact]
    public async Task GetEnrichedByDateAsync_WithMedicoUserId_ResolvesMedicoByGuid()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var medicoUserId = Guid.NewGuid();
        var medicoGuid = Guid.NewGuid();
        var medico = new Medico("Dr. Enriquecido", 1, medicoUserId);
        medico.SetIdGuid(medicoGuid);
        var appointment = new AppointmentBuilder()
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(Guid.NewGuid())
            .WithMedicoUserId(medicoUserId)
            .Build();

        _fixture.AppointmentRepository
            .GetByRangeAsync(date, date, 0, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _fixture.AppointmentRepository
            .CountByRangeAsync(date, date, Arg.Any<CancellationToken>())
            .Returns(1);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());
        _fixture.MedicoRepository
            .GetByMedicoUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([medico]);
        _fixture.MedicoRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.PatientRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.CameraRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.BlockHistoryRepository
            .GetByRangeAsync(date, date, null, Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetEnrichedByDateAsync(date, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.NotNull(summary.Medico);
        Assert.Equal(medicoGuid, summary.Medico.Id);
        Assert.Equal(medico.Nombre, summary.Medico.Nombre);
    }

    [Fact]
    public async Task GetEnrichedByDateAsync_WithOnlyMedicoId_FallsBackToIntLookup()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var medicoId = 77;
        var medicoGuid = Guid.NewGuid();
        var medico = new Medico("Dr. IntLookup", 2);
        typeof(Medico).GetProperty(nameof(Medico.Id))?.SetValue(medico, medicoId);
        medico.SetIdGuid(medicoGuid);
        var appointment = new AppointmentBuilder()
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(Guid.NewGuid())
            .WithMedicoId(medicoId)
            .Build();

        _fixture.AppointmentRepository
            .GetByRangeAsync(date, date, 0, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _fixture.AppointmentRepository
            .CountByRangeAsync(date, date, Arg.Any<CancellationToken>())
            .Returns(1);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());
        _fixture.MedicoRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([medico]);
        _fixture.MedicoRepository
            .GetByMedicoUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.PatientRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.CameraRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.BlockHistoryRepository
            .GetByRangeAsync(date, date, null, Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetEnrichedByDateAsync(date, CancellationToken.None);

        // Assert
        var summary = Assert.Single(result);
        Assert.NotNull(summary.Medico);
        Assert.Equal(medicoGuid, summary.Medico.Id);
        Assert.Equal(medico.Nombre, summary.Medico.Nombre);
    }

    #endregion
}
