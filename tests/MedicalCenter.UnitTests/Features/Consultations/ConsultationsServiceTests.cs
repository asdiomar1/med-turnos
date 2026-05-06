using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Consultations;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Consultations;

public sealed class ConsultationsServiceTests
{
    private readonly IConsultationRepository consultationRepository = Substitute.For<IConsultationRepository>();
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly IPatientRepository patientRepository = Substitute.For<IPatientRepository>();
    private readonly IMedicoRepository medicoRepository = Substitute.For<IMedicoRepository>();
    private readonly IClinicalHistoryRepository clinicalHistoryRepository = Substitute.For<IClinicalHistoryRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIdempotencyStore idempotencyStore = Substitute.For<IIdempotencyStore>();
    private readonly IClock clock = Substitute.For<IClock>();

    private readonly DateTimeOffset now = new(2026, 05, 01, 10, 0, 0, TimeSpan.Zero);

    private ConsultationsService Sut => new(
        new ConsultationsDataAccessDependencies(
            consultationRepository,
            userRepository,
            patientRepository,
            medicoRepository,
            clinicalHistoryRepository,
            unitOfWork,
            idempotencyStore),
        new ConsultationsRuntimeDependencies(clock));

    public ConsultationsServiceTests()
    {
        clock.UtcNow.Returns(now);
    }

    [Fact]
    public async Task CreateScheduleHourAsync_WithInvariantTime_UsesExpectedHour()
    {
        consultationRepository.GetNextScheduleHourIdAsync(Arg.Any<CancellationToken>()).Returns(55);

        var result = await Sut.CreateScheduleHourAsync(new ConsultationScheduleHourUpsertCommand("09:30", 1), CancellationToken.None);

        Assert.Equal("09:30", result.Hora);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetScheduleHoursAsync_ReturnsMappedSummaries()
    {
        consultationRepository.GetScheduleHoursAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new ConsultationScheduleHour(1, "08:00", 1, true),
            new ConsultationScheduleHour(2, "09:00", 2, false)
        ]);

        var result = await Sut.GetScheduleHoursAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == 1 && x.Hora == "08:00" && x.Activo);
    }

    [Fact]
    public async Task CreateScheduleHourAsync_WithInvalidHour_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Sut.CreateScheduleHourAsync(new ConsultationScheduleHourUpsertCommand("99:99", 1), CancellationToken.None));

        await consultationRepository.DidNotReceiveWithAnyArgs().AddScheduleHourAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateScheduleHourAsync_WhenNotFound_ThrowsNotFoundException()
    {
        consultationRepository.GetScheduleHourByIdAsync(10, Arg.Any<CancellationToken>()).Returns((ConsultationScheduleHour?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.UpdateScheduleHourAsync(10, new ConsultationScheduleHourUpsertCommand("10:00", 1), CancellationToken.None));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateScheduleHourAsync_WhenFound_UpdatesAndSaves()
    {
        var entity = new ConsultationScheduleHour(10, "10:00", 1, true);
        consultationRepository.GetScheduleHourByIdAsync(10, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await Sut.UpdateScheduleHourAsync(10, new ConsultationScheduleHourUpsertCommand("10:30", 2), CancellationToken.None);

        Assert.Equal("10:30", result.Hora);
        Assert.Equal(2, result.Orden);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteScheduleHourAsync_WithFutureSlots_ThrowsConflictException()
    {
        var hour = new ConsultationScheduleHour(7, "11:00", 1, true);
        consultationRepository.GetScheduleHourByIdAsync(7, Arg.Any<CancellationToken>()).Returns(hour);
        consultationRepository.CountFutureSlotsByHourAsync(new TimeOnly(11, 0), DateOnly.FromDateTime(now.UtcDateTime.Date), Arg.Any<CancellationToken>()).Returns(2);

        await Assert.ThrowsAsync<ConflictException>(() => Sut.DeleteScheduleHourAsync(7, CancellationToken.None));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleScheduleHourAsync_WithExistingHour_TogglesAndSaves()
    {
        var hour = new ConsultationScheduleHour(4, "10:00", 1, true);
        consultationRepository.GetScheduleHourByIdAsync(4, Arg.Any<CancellationToken>()).Returns(hour);

        var result = await Sut.ToggleScheduleHourAsync(4, false, CancellationToken.None);

        Assert.False(result.Activo);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleScheduleHourAsync_WhenNotFound_ThrowsNotFoundException()
    {
        consultationRepository.GetScheduleHourByIdAsync(999, Arg.Any<CancellationToken>()).Returns((ConsultationScheduleHour?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.ToggleScheduleHourAsync(999, true, CancellationToken.None));
    }

    [Fact]
    public async Task PreviewDeleteScheduleHourAsync_WithNoFutureSlots_AllowsDelete()
    {
        var hour = new ConsultationScheduleHour(5, "12:00", 1, true);
        consultationRepository.GetScheduleHourByIdAsync(5, Arg.Any<CancellationToken>()).Returns(hour);
        consultationRepository.CountFutureSlotsByHourAsync(new TimeOnly(12, 0), DateOnly.FromDateTime(now.UtcDateTime.Date), Arg.Any<CancellationToken>()).Returns(0);

        var result = await Sut.PreviewDeleteScheduleHourAsync(5, CancellationToken.None);

        Assert.True(result.CanDelete);
        Assert.Equal(0, result.FutureSlotsCount);
    }

    [Fact]
    public async Task PreviewDeleteScheduleHourAsync_WhenHourMissing_ThrowsNotFoundException()
    {
        consultationRepository.GetScheduleHourByIdAsync(51, Arg.Any<CancellationToken>()).Returns((ConsultationScheduleHour?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.PreviewDeleteScheduleHourAsync(51, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteScheduleHourAsync_WithoutFutureSlots_DisablesHourAndSaves()
    {
        var hour = new ConsultationScheduleHour(8, "13:00", 1, true);
        consultationRepository.GetScheduleHourByIdAsync(8, Arg.Any<CancellationToken>()).Returns(hour);
        consultationRepository.CountFutureSlotsByHourAsync(new TimeOnly(13, 0), DateOnly.FromDateTime(now.UtcDateTime.Date), Arg.Any<CancellationToken>()).Returns(0);

        var result = await Sut.DeleteScheduleHourAsync(8, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.Activo);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_WithActiveAndExistingHours_InsertsOnlyMissingSlots()
    {
        var date = new DateOnly(2026, 5, 2);
        consultationRepository.GetScheduleHoursAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new ConsultationScheduleHour(1, "09:00", 1, true),
            new ConsultationScheduleHour(2, "10:00", 2, true),
            new ConsultationScheduleHour(3, "11:00", 3, false)
        ]);
        consultationRepository.GetByDateAsync(date, Arg.Any<CancellationToken>()).Returns([
            new ConsultationSlot(Guid.NewGuid(), date, new TimeOnly(9, 0))
        ]);

        var inserted = await Sut.GenerateAsync(date, CancellationToken.None);

        Assert.Equal(1, inserted);
        await consultationRepository.Received(1).AddAsync(Arg.Is<ConsultationSlot>(x => x.Hora == new TimeOnly(10, 0)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByDateAsync_WhenDateIsNull_ReturnsEmptyAndDoesNotHitRepository()
    {
        var result = await Sut.GetByDateAsync(null, CancellationToken.None);

        Assert.Empty(result);
        await consultationRepository.DidNotReceiveWithAnyArgs().GetByDateAsync(default, default);
    }

    [Fact]
    public async Task GetByDateAsync_WithDate_ReturnsMappedSlots()
    {
        var date = new DateOnly(2026, 5, 2);
        var slot = new ConsultationSlot(Guid.NewGuid(), date, new TimeOnly(14, 0));
        consultationRepository.GetByDateAsync(date, Arg.Any<CancellationToken>()).Returns([slot]);

        var result = await Sut.GetByDateAsync(date, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("libre", result.First().Estado);
    }

    [Fact]
    public async Task RepairRangeAsync_WhenEndDateBeforeStart_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Sut.RepairRangeAsync(new DateOnly(2026, 5, 3), new DateOnly(2026, 5, 2), CancellationToken.None));
    }

    [Fact]
    public async Task RepairRangeAsync_WithValidRange_ReturnsAccumulatedInserted()
    {
        var start = new DateOnly(2026, 5, 2);
        var end = new DateOnly(2026, 5, 3);
        consultationRepository.GetScheduleHoursAsync(Arg.Any<CancellationToken>()).Returns([new ConsultationScheduleHour(1, "09:00", 1, true)]);
        consultationRepository.GetByDateAsync(start, Arg.Any<CancellationToken>()).Returns([]);
        consultationRepository.GetByDateAsync(end, Arg.Any<CancellationToken>()).Returns([]);

        var total = await Sut.RepairRangeAsync(start, end, CancellationToken.None);

        Assert.Equal(2, total);
        await consultationRepository.Received(2).AddAsync(Arg.Any<ConsultationSlot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByRangeAsync_WhenRepositoryReturnsEmpty_ReturnsEmpty()
    {
        consultationRepository.GetByRangeAsync(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2), Arg.Any<CancellationToken>()).Returns([]);

        var result = await Sut.GetByRangeAsync(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AssignAsync_WithoutMedico_ThrowsValidationExceptionAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.asignar"));
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(BuildPatient(patientId));
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-1", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        var command = new AssignConsultationCommand(patientId, MedicoId: null, ObservacionesAdmin: null, MedicoUserId: null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Sut.AssignAsync(actorUserId, slotId, "idem-1", command, CancellationToken.None));

        Assert.Contains("requerido", exception.Message, StringComparison.OrdinalIgnoreCase);
        await idempotencyStore.Received(1).FailAsync($"consultas.asignaciones:{slotId}", "idem-1", Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignAsync_WithValidData_AssignsAndCompletesIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var slotDate = new DateOnly(2026, 5, 2);
        var slot = new ConsultationSlot(slotId, slotDate, new TimeOnly(12, 0));

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.asignar"));
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(BuildPatient(patientId));
        var medicoUserId = Guid.NewGuid();
        userRepository.GetByIdAsync(medicoUserId, Arg.Any<CancellationToken>()).Returns(BuildMedicoUser(medicoUserId));
        consultationRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(slot);
        consultationRepository.GetByDateAsync(slotDate, Arg.Any<CancellationToken>()).Returns([]);
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-ok", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        var result = await Sut.AssignAsync(actorUserId, slotId, "idem-ok", new AssignConsultationCommand(patientId, null, " obs ", medicoUserId), CancellationToken.None);

        Assert.Equal("confirmada", result.Estado);
        Assert.Equal(patientId, result.PacienteId);
        Assert.Equal(medicoUserId, result.MedicoUserId);
        await idempotencyStore.Received(1).CompleteAsync($"consultas.asignaciones:{slotId}", "idem-ok", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenSlotNotFound_ThrowsAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.cancelar"));
        consultationRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((ConsultationSlot?)null);
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-cancel", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.CancelAsync(actorUserId, slotId, "idem-cancel", new CancelConsultationCommand("motivo"), CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync($"consultas.cancelaciones:{slotId}", "idem-cancel", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WithAssignedSlot_CancelsAndCompletesIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var slot = CreateConfirmedSlot(slotId, patientId, 9);

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.cancelar"));
        consultationRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(slot);
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-cancel-ok", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        var result = await Sut.CancelAsync(actorUserId, slotId, "idem-cancel-ok", new CancelConsultationCommand("x"), CancellationToken.None);

        Assert.Equal("cancelada", result.Estado);
        await idempotencyStore.Received(1).CompleteAsync($"consultas.cancelaciones:{slotId}", "idem-cancel-ok", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescheduleAsync_WhenSourceNotFound_ThrowsAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.reprogramar"));
        consultationRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((ConsultationSlot?)null);
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-res", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.RescheduleAsync(actorUserId, slotId, "idem-res", new RescheduleConsultationCommand(targetSlotId, null, null), CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync($"consultas.reprogramaciones:{slotId}", "idem-res", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescheduleAsync_WithValidSourceAndTarget_ReschedulesAndCompletesIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 2);
        var source = CreateConfirmedSlot(sourceId, patientId, 9);
        var target = new ConsultationSlot(targetId, date, new TimeOnly(11, 0));

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.reprogramar"));
        consultationRepository.GetByIdAsync(sourceId, Arg.Any<CancellationToken>()).Returns(source);
        consultationRepository.GetByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(target);
        consultationRepository.GetByDateAsync(date, Arg.Any<CancellationToken>()).Returns([]);
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-res-ok", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        var result = await Sut.RescheduleAsync(actorUserId, sourceId, "idem-res-ok", new RescheduleConsultationCommand(targetId, 4, null), CancellationToken.None);

        Assert.Equal(targetId, result.Id);
        Assert.Equal("confirmada", result.Estado);
        await idempotencyStore.Received(1).CompleteAsync($"consultas.reprogramaciones:{sourceId}", "idem-res-ok", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseAsync_WhenSlotNotFound_ThrowsAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.cerrar"));
        consultationRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((ConsultationSlot?)null);
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-close", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.CloseAsync(actorUserId, slotId, "idem-close", new CloseConsultationCommand("completada", null, null, null, null), CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync($"consultas.cierres:{slotId}", "idem-close", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseAsync_WithConfirmedSlot_CompletesConsultationAndCompletesIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var slot = CreateConfirmedSlot(slotId, patientId, 6);

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(BuildStaffActor(actorUserId, "consultas.cerrar"));
        consultationRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(slot);
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-close-ok", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        var result = await Sut.CloseAsync(actorUserId, slotId, "idem-close-ok", new CloseConsultationCommand("completada", null, null, null, null), CancellationToken.None);

        Assert.Equal("completada", result.Estado);
        await idempotencyStore.Received(1).CompleteAsync($"consultas.cierres:{slotId}", "idem-close-ok", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCompletedSessionsAsync_WithSessions_ReturnsMappedResults()
    {
        var patientId = Guid.NewGuid();
        consultationRepository.GetSessionsByPatientIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(
        [
            new ConsultationSession(new ConsultationSessionCreateParams(
                Guid.NewGuid(),
                patientId,
                Guid.NewGuid(),
                new DateOnly(2026, 5, 1),
                new TimeOnly(8, 0),
                null,
                "particular",
                null,
                null,
                null,
                null,
                null))
        ]);

        var result = await Sut.GetCompletedSessionsAsync(patientId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(patientId, result.First().PacienteId);
    }

    private static User BuildStaffActor(Guid actorUserId, string permission)
    {
        var actor = new User(new UserCreateParams(actorUserId, "staff", "staff@medicalcenter.local", "hash", IsActive: true, IsStaff: true));
        actor.SetRoles([new Role(new RoleCreateParams(Guid.NewGuid(), "staff", "Staff", [permission]))]);
        return actor;
    }

    private static Patient BuildPatient(Guid patientId) =>
        new(patientId, "Paciente", new PatientAdministrativeInfo("111111", "123", "123", 1), new PatientPortalInfo(false));

    private static User BuildMedicoUser(Guid medicoUserId)
    {
        var medico = new User(new UserCreateParams(medicoUserId, "medico", "medico@medicalcenter.local", "hash", IsActive: true, IsStaff: true));
        medico.SetRoles([new Role(new RoleCreateParams(Guid.NewGuid(), "medico", "Medico", []))]);
        return medico;
    }

    private ConsultationSlot CreateConfirmedSlot(Guid slotId, Guid patientId, int medicoId)
    {
        var slot = new ConsultationSlot(slotId, new DateOnly(2026, 5, 2), new TimeOnly(10, 0));
        slot.Assign(patientId, medicoId, null, Guid.NewGuid(), now);
        return slot;
    }
}
