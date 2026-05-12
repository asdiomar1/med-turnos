using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Application.Features.DailyClosings;
using MedicalCenter.Application.Features.OutOfHoursTurns;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.DailyClosings;

public sealed class DailyClosingsServiceTests
{
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly IAppointmentRepository appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IDailyClosingRepository dailyClosingRepository = Substitute.For<IDailyClosingRepository>();
    private readonly IAppointmentsService appointmentsService = Substitute.For<IAppointmentsService>();
    private readonly IOutOfHoursTurnsService outOfHoursTurnsService = Substitute.For<IOutOfHoursTurnsService>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task PreviewAsync_WithStandardAndExtraTurns_ExcludesExtrasFromMetricsAndAppendsExtras()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var patientId = Guid.NewGuid();
        var earlyExtraId = Guid.NewGuid();
        var lateStandardId = Guid.NewGuid();
        var earlyStandardId = Guid.NewGuid();
        appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(
        [
            CreateAppointment(fecha, AppointmentStatus.Libre),
            CreateAppointment(fecha, AppointmentStatus.Ocupado),
            CreateAppointment(fecha, AppointmentStatus.Apartado),
            CreateAppointment(fecha, AppointmentStatus.Cancelado)
        ]);
        appointmentsService.GetEnrichedByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(
        [
            CreateStandardTurn(lateStandardId, fecha, new TimeOnly(10, 0), camaraId: 2, lugar: 1, patientId),
            CreateStandardTurn(earlyStandardId, fecha, new TimeOnly(9, 0), camaraId: 1, lugar: 1, patientId)
        ]);
        outOfHoursTurnsService.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(
        [
            CreateExtraTurn(earlyExtraId, fecha, new TimeOnly(8, 0), patientId)
        ]);

        var result = await CreateSut().PreviewAsync(fecha, CancellationToken.None);

        Assert.Equal(fecha, result.Fecha);
        Assert.Equal(4, result.TotalTurnos);
        Assert.Equal(1, result.Libres);
        Assert.Equal(1, result.Ocupados);
        Assert.Equal(1, result.Apartados);
        Assert.Equal(1, result.Cancelados);
        Assert.Equal(25m, result.OcupacionPorcentaje);
        Assert.True(result.AptoParaCierre);
        Assert.Contains(result.Alertas, x => x.Code == "apartados_pending" && x.Count == 1);
        Assert.Contains(result.Alertas, x => x.Code == "cancelled_turnos" && x.Count == 1);
        Assert.Equal([earlyStandardId, lateStandardId, earlyExtraId], result.Turnos.Select(x => x.SlotId ?? x.TurnoFueraHorarioId).ToArray());
        Assert.Equal(["09:00", "10:00", "08:00"], result.Turnos.Select(x => x.Hora).ToArray());
        Assert.Equal(3, result.Turnos.Last().PacienteNumeroDia);
    }

    [Fact]
    public async Task PreviewAsync_WithEmptyAppointmentDay_ReturnsNoAppointmentsAlertAndNonDefaultTimestamp()
    {
        var fecha = new DateOnly(2026, 5, 12);
        var before = DateTimeOffset.UtcNow;
        appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns([]);
        appointmentsService.GetEnrichedByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns([]);
        outOfHoursTurnsService.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().PreviewAsync(fecha, CancellationToken.None);

        var after = DateTimeOffset.UtcNow;
        Assert.Equal(0, result.TotalTurnos);
        Assert.False(result.AptoParaCierre);
        var alert = Assert.Single(result.Alertas);
        Assert.Equal("day_without_slots", alert.Code);
        Assert.Equal("warning", alert.Severity);
        Assert.InRange(result.GeneradoEn, before, after);
        Assert.Empty(result.Turnos);
    }

    [Fact]
    public async Task PreviewAsync_EnforcesReferidoInvariantsAndModalidadDefault()
    {
        var fecha = new DateOnly(2026, 5, 22);
        var patientId = Guid.NewGuid();
        appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(
        [
            CreateAppointment(fecha, AppointmentStatus.Ocupado),
            CreateAppointment(fecha, AppointmentStatus.Ocupado),
            CreateAppointment(fecha, AppointmentStatus.Ocupado)
        ]);

        appointmentsService.GetEnrichedByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(
        [
            CreateStandardTurn(Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, 1, patientId, modalidadCobro: null, referidoTercero: false, referenteId: 99, referenteNombre: "No debería salir", referenteTipo: "agencia"),
            CreateStandardTurn(Guid.NewGuid(), fecha, new TimeOnly(10, 0), 1, 2, patientId, modalidadCobro: " ", referidoTercero: true, referenteId: 55, referenteNombre: "Derivador", referenteTipo: "AGENCIA"),
            CreateStandardTurn(Guid.NewGuid(), fecha, new TimeOnly(11, 0), 1, 3, patientId, modalidadCobro: "obra_social", referidoTercero: true, referenteId: null, referenteNombre: "Sin id legacy", referenteTipo: "inventado")
        ]);
        outOfHoursTurnsService.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().PreviewAsync(fecha, CancellationToken.None);
        var turnos = result.Turnos.ToArray();

        var noReferido = turnos[0];
        Assert.False(noReferido.ReferidoTercero);
        Assert.Null(noReferido.ReferenteId);
        Assert.Null(noReferido.ReferenteNombre);
        Assert.Null(noReferido.ReferenteTipo);
        Assert.Equal("particular", noReferido.ModalidadCobro);

        var referidoValido = turnos[1];
        Assert.True(referidoValido.ReferidoTercero);
        Assert.Equal(55, referidoValido.ReferenteId);
        Assert.Equal("Derivador", referidoValido.ReferenteNombre);
        Assert.Equal("agencia", referidoValido.ReferenteTipo);
        Assert.Equal("particular", referidoValido.ModalidadCobro);

        var referidoLegacy = turnos[2];
        Assert.True(referidoLegacy.ReferidoTercero);
        Assert.Null(referidoLegacy.ReferenteId);
        Assert.Equal("Sin id legacy", referidoLegacy.ReferenteNombre);
        Assert.Null(referidoLegacy.ReferenteTipo);
        Assert.Equal("obra_social", referidoLegacy.ModalidadCobro);
    }

    [Fact]
    public async Task ConfirmAsync_WhenActorMissing_ThrowsUnauthorizedAndDoesNotCommit()
    {
        var actorUserId = Guid.NewGuid();
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateSut().ConfirmAsync(actorUserId, new DateOnly(2026, 5, 11), null, CancellationToken.None));

        await dailyClosingRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ConfirmAsync_WhenActorCannotManageStaff_ThrowsForbiddenAndDoesNotCommit(bool isStaff, bool includePermission)
    {
        var actorUserId = Guid.NewGuid();
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(CreateActor(actorUserId, isStaff, includePermission));

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateSut().ConfirmAsync(actorUserId, new DateOnly(2026, 5, 11), null, CancellationToken.None));

        await dailyClosingRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAsync_WhenNoClosingExists_AddsConfirmedClosingWithObjectDetailsAndSaves()
    {
        var actorUserId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 13);
        DailyClosing? added = null;
        SetupAuthorizedActor(actorUserId);
        dailyClosingRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns((DailyClosing?)null);
        dailyClosingRepository.AddAsync(Arg.Do<DailyClosing>(x => added = x), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await CreateSut().ConfirmAsync(actorUserId, fecha, "{\"note\":\"ok\"}", CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(result.Id, added.Id);
        Assert.Equal(fecha, added.Fecha);
        Assert.Equal(DailyClosingStatus.Confirmed, added.Status);
        Assert.Equal(actorUserId, added.CreatedByUserId);
        Assert.Equal(actorUserId, added.ConfirmedByUserId);
        Assert.Equal("{\"note\":\"ok\"}", added.DetallesJson);
        Assert.Equal("confirmed", result.Estado);
        await dailyClosingRepository.Received(1).AddAsync(added, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAsync_WhenClosingExistsByDate_UpdatesExistingClosingAndSaves()
    {
        var actorUserId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 14);
        var closing = CreateClosing(fecha: fecha, detallesJson: "{\"previous\":true}");
        SetupAuthorizedActor(actorUserId);
        dailyClosingRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(closing);

        var result = await CreateSut().ConfirmAsync(actorUserId, fecha, "{\"updated\":true}", CancellationToken.None);

        Assert.Equal(closing.Id, result.Id);
        Assert.Equal(DailyClosingStatus.Confirmed, closing.Status);
        Assert.Equal(actorUserId, closing.ConfirmedByUserId);
        Assert.Equal("{\"updated\":true}", closing.DetallesJson);
        await dailyClosingRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAsync_WhenClosingIdProvided_UpdatesSelectedClosingAndSaves()
    {
        var actorUserId = Guid.NewGuid();
        var requestedDate = new DateOnly(2026, 5, 14);
        var closing = CreateClosing(fecha: requestedDate.AddDays(-1));
        SetupAuthorizedActor(actorUserId);
        dailyClosingRepository.GetByIdAsync(closing.Id, Arg.Any<CancellationToken>()).Returns(closing);

        var result = await CreateSut().ConfirmAsync(actorUserId, requestedDate, closing.Id, null, CancellationToken.None);

        Assert.Equal(closing.Id, result.Id);
        Assert.Equal(closing.Fecha, result.Fecha);
        Assert.Equal(DailyClosingStatus.Confirmed, closing.Status);
        Assert.Equal(actorUserId, closing.ConfirmedByUserId);
        await dailyClosingRepository.DidNotReceiveWithAnyArgs().GetByDateAsync(default, default);
        await dailyClosingRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAsync_WithInvalidDetailsForNewClosing_LeavesDetailsNull()
    {
        var actorUserId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 15);
        DailyClosing? added = null;
        SetupAuthorizedActor(actorUserId);
        dailyClosingRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns((DailyClosing?)null);
        dailyClosingRepository.AddAsync(Arg.Do<DailyClosing>(x => added = x), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await CreateSut().ConfirmAsync(actorUserId, fecha, "not-json", CancellationToken.None);

        Assert.NotNull(added);
        Assert.Null(added.DetallesJson);
        Assert.Null(result.DetallesJson);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAsync_WithNonObjectDetailsForExistingClosing_PreservesPreviousDetails()
    {
        var actorUserId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 16);
        var closing = CreateClosing(fecha: fecha, detallesJson: "{\"previous\":true}");
        SetupAuthorizedActor(actorUserId);
        dailyClosingRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(closing);

        var result = await CreateSut().ConfirmAsync(actorUserId, fecha, "[\"not\",\"object\"]", CancellationToken.None);

        Assert.Equal("{\"previous\":true}", closing.DetallesJson);
        Assert.Equal("{\"previous\":true}", result.DetallesJson);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDetailAsync_WhenFoundByDateOrId_ReturnsSelectedClosing()
    {
        var fecha = new DateOnly(2026, 5, 17);
        var dateClosing = CreateClosing(fecha: fecha, detallesJson: "{\"date\":true}");
        var idClosing = CreateClosing(fecha: fecha.AddDays(-1), detallesJson: "{\"id\":true}");
        dailyClosingRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns(dateClosing);
        dailyClosingRepository.GetByIdAsync(idClosing.Id, Arg.Any<CancellationToken>()).Returns(idClosing);

        var byDate = await CreateSut().GetDetailAsync(fecha, null, CancellationToken.None);
        var byId = await CreateSut().GetDetailAsync(fecha, idClosing.Id, CancellationToken.None);

        Assert.Equal(dateClosing.Id, byDate.Id);
        Assert.Equal(fecha, byDate.Fecha);
        Assert.Equal("{\"date\":true}", byDate.DetallesJson);
        Assert.Equal(idClosing.Id, byId.Id);
        Assert.Equal(idClosing.Fecha, byId.Fecha);
        Assert.Equal("{\"id\":true}", byId.DetallesJson);
    }

    [Fact]
    public async Task GetDetailAsync_WhenMissing_ReturnsSinCierreFallback()
    {
        var fecha = new DateOnly(2026, 5, 18);
        dailyClosingRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns((DailyClosing?)null);

        var result = await CreateSut().GetDetailAsync(fecha, null, CancellationToken.None);

        Assert.Null(result.Id);
        Assert.Equal(fecha, result.Fecha);
        Assert.Equal("sin_cierre", result.Estado);
        Assert.Null(result.DetallesJson);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetMonthlyExportAsync_WithInvalidMonth_ThrowsValidationException(int month)
    {
        await Assert.ThrowsAsync<ValidationException>(() => CreateSut().GetMonthlyExportAsync(2026, month, CancellationToken.None));

        await dailyClosingRepository.DidNotReceiveWithAnyArgs().GetByMonthAsync(default, default, default);
    }

    [Fact]
    public async Task GetMonthlyExportAsync_WithValidMonth_ReturnsMappedClosings()
    {
        var actorUserId = Guid.NewGuid();
        var first = CreateClosing(fecha: new DateOnly(2026, 5, 1));
        var second = CreateClosing(fecha: new DateOnly(2026, 5, 2));
        first.Confirm(actorUserId, "{\"first\":true}");
        dailyClosingRepository.GetByMonthAsync(2026, 5, Arg.Any<CancellationToken>()).Returns([first, second]);

        var result = await CreateSut().GetMonthlyExportAsync(2026, 5, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == first.Id && x.Estado == "confirmed" && x.DetallesJson == "{\"first\":true}");
        Assert.Contains(result, x => x.Id == second.Id && x.Estado == "pending");
    }

    [Fact]
    public async Task ReopenAsync_WhenActorMissing_ThrowsUnauthorizedAndDoesNotCommit()
    {
        var actorUserId = Guid.NewGuid();
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateSut().ReopenAsync(actorUserId, new DateOnly(2026, 5, 19), null, "motivo", CancellationToken.None));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReopenAsync_WhenClosingMissing_ThrowsNotFoundAndDoesNotCommit()
    {
        var actorUserId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 20);
        SetupAuthorizedActor(actorUserId);
        dailyClosingRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns((DailyClosing?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().ReopenAsync(actorUserId, fecha, null, "motivo", CancellationToken.None));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReopenAsync_WithAuthorizedActorAndClosingId_TrimsReasonReopensAndSaves()
    {
        var actorUserId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 21);
        var closing = CreateClosing(fecha: fecha, detallesJson: "{\"closed\":true}");
        SetupAuthorizedActor(actorUserId);
        dailyClosingRepository.GetByIdAsync(closing.Id, Arg.Any<CancellationToken>()).Returns(closing);

        var result = await CreateSut().ReopenAsync(actorUserId, fecha, closing.Id, "  Recalcular caja  ", CancellationToken.None);

        Assert.Equal(DailyClosingStatus.Reopened, closing.Status);
        Assert.Equal(actorUserId, closing.ReopenedByUserId);
        Assert.Equal("Recalcular caja", closing.MotivoReapertura);
        Assert.Equal("reopened", result.Estado);
        Assert.Equal("Recalcular caja", result.MotivoReapertura);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private DailyClosingsService CreateSut() => new(
        userRepository,
        appointmentRepository,
        dailyClosingRepository,
        appointmentsService,
        outOfHoursTurnsService,
        unitOfWork);

    private void SetupAuthorizedActor(Guid actorUserId)
    {
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(CreateActor(actorUserId, isStaff: true, includePermission: true));
    }

    private static User CreateActor(Guid id, bool isStaff, bool includePermission)
    {
        var builder = new UserBuilder().WithId(id);
        builder = isStaff ? builder.AsStaff() : builder.AsPatient();
        return includePermission ? builder.WithPermission("staff.manage").Build() : builder.Build();
    }

    private static Appointment CreateAppointment(DateOnly fecha, AppointmentStatus status) =>
        new AppointmentBuilder()
            .WithFecha(fecha)
            .WithStatus(status)
            .Build();

    private static DailyClosing CreateClosing(Guid? id = null, DateOnly? fecha = null, Guid? createdByUserId = null, string? detallesJson = null) =>
        new(id ?? Guid.NewGuid(), fecha ?? new DateOnly(2026, 5, 11), createdByUserId ?? Guid.NewGuid(), detallesJson);

    private static TurnoEnrichedSummary CreateStandardTurn(
        Guid id,
        DateOnly fecha,
        TimeOnly hora,
        int camaraId,
        int lugar,
        Guid patientId,
        string? modalidadCobro = "particular",
        bool? referidoTercero = false,
        int? referenteId = null,
        string? referenteNombre = null,
        string? referenteTipo = null) =>
        new(
            Id: id,
            Fecha: fecha,
            Hora: hora,
            CamaraId: camaraId,
            Lugar: lugar,
            Estado: "ocupado",
            PacienteId: patientId,
            EsTanda: false,
            TandaId: null,
            EsBloqueCompleto: false,
            ReferidoTercero: referidoTercero,
            ReferenteId: referenteId,
            ModalidadCobro: modalidadCobro,
            ObraSocialId: null,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            MedicoId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            ObraSocialValidadaPor: null,
            ObraSocialValidadaAt: null,
            Paciente: new PacienteEnrichedSummary(patientId, "Paciente", null, null),
            Medico: null,
            Referente: referenteId.HasValue || !string.IsNullOrWhiteSpace(referenteNombre) || !string.IsNullOrWhiteSpace(referenteTipo)
                ? new ReferenteEnrichedSummary(referenteId ?? 0, referenteNombre, referenteTipo, true)
                : null,
            Camara: new CamaraEnrichedSummary(camaraId, $"Camara {camaraId}", 1),
            ObraSocial: null,
            ObraSocialValidadaPorPerfil: null);

    private static OutOfHoursTurnSummary CreateExtraTurn(Guid id, DateOnly fecha, TimeOnly hora, Guid patientId) =>
        new(
            id,
            fecha,
            hora,
            patientId,
            "Extra",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            EsMonoxido: true,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            MonoxidoMedicoId: 7,
            MonoxidoMedicoUserId: null,
            Paciente: new GuidLookupSummary(patientId, "Paciente Extra"),
            MonoxidoMedico: new IntLookupSummary(7, "Dr. Extra"),
            MonoxidoMedicoUser: null,
            OperadorCamara: null);
}
