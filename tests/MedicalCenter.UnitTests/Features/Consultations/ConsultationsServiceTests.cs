using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Consultations;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Consultations;

public sealed class ConsultationsServiceTests
{
    [Fact]
    public async Task AssignAsync_WithoutMedico_ThrowsValidationExceptionAndFailsIdempotency()
    {
        var consultationRepository = Substitute.For<IConsultationRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var patientRepository = Substitute.For<IPatientRepository>();
        var medicoRepository = Substitute.For<IMedicoRepository>();
        var clinicalHistoryRepository = Substitute.For<IClinicalHistoryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var idempotencyStore = Substitute.For<IIdempotencyStore>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var service = new ConsultationsService(
            new ConsultationsDataAccessDependencies(
                consultationRepository,
                userRepository,
                patientRepository,
                medicoRepository,
                clinicalHistoryRepository,
                unitOfWork,
                idempotencyStore),
            new ConsultationsRuntimeDependencies(clock));

        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var actor = BuildStaffActor(actorUserId, "consultas.asignar");

        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(actor);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(
            new Patient(
                patientId,
                "Paciente",
                new PatientAdministrativeInfo("111111", "123", "123", 1),
                new PatientPortalInfo(false)));
        idempotencyStore.ReserveAsync(Arg.Any<string>(), "idem-1", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));

        var command = new AssignConsultationCommand(patientId, MedicoId: null, ObservacionesAdmin: null, MedicoUserId: null);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.AssignAsync(actorUserId, slotId, "idem-1", command, CancellationToken.None));

        Assert.Contains("requerido", exception.Message, StringComparison.OrdinalIgnoreCase);
        await idempotencyStore.Received(1).FailAsync($"consultas.asignaciones:{slotId}", "idem-1", Arg.Any<CancellationToken>());
        await consultationRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateScheduleHourAsync_WithInvariantTime_UsesExpectedHour()
    {
        var consultationRepository = Substitute.For<IConsultationRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var patientRepository = Substitute.For<IPatientRepository>();
        var medicoRepository = Substitute.For<IMedicoRepository>();
        var clinicalHistoryRepository = Substitute.For<IClinicalHistoryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var idempotencyStore = Substitute.For<IIdempotencyStore>();
        var clock = Substitute.For<IClock>();

        consultationRepository.GetNextScheduleHourIdAsync(Arg.Any<CancellationToken>()).Returns(55);

        var service = new ConsultationsService(
            new ConsultationsDataAccessDependencies(
                consultationRepository,
                userRepository,
                patientRepository,
                medicoRepository,
                clinicalHistoryRepository,
                unitOfWork,
                idempotencyStore),
            new ConsultationsRuntimeDependencies(clock));

        var result = await service.CreateScheduleHourAsync(
            new ConsultationScheduleHourUpsertCommand("09:30", 1),
            CancellationToken.None);

        Assert.Equal("09:30", result.Hora);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static User BuildStaffActor(Guid actorUserId, string permission)
    {
        var actor = new User(new UserCreateParams(actorUserId, "staff", "staff@medicalcenter.local", "hash", IsActive: true, IsStaff: true));
        actor.SetRoles([
            new Role(new RoleCreateParams(Guid.NewGuid(), "staff", "Staff", [permission]))
        ]);

        return actor;
    }
}
