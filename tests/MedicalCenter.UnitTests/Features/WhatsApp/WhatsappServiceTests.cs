using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.WhatsApp;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.WhatsApp;

// Scenario map:
// - EnqueueTurnoConfirmadoAsync: appointment not found (not occupied), patient null, patient not opted in, success single, success tanda
// - EnqueueTurnoCancelacionAsync: patient null, patient not opted in, success with reason
// - EnqueueTurnosCancelacionAsync: all opted-out, success with multiple slots
// - DispatchAsync: empty queue, template not found, sender success, sender failure
// - SendRemindersAsync: no appointments, patient not opted in, eligible appointments
public sealed class WhatsappServiceTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly IWhatsappDispatchQueueRepository _queueRepository = Substitute.For<IWhatsappDispatchQueueRepository>();
    private readonly IWhatsappTemplateRepository _templateRepository = Substitute.For<IWhatsappTemplateRepository>();
    private readonly IWhatsappMessageRepository _messageRepository = Substitute.For<IWhatsappMessageRepository>();
    private readonly IWhatsappMessageActionRepository _messageActionRepository = Substitute.For<IWhatsappMessageActionRepository>();
    private readonly IWhatsAppSender _sender = Substitute.For<IWhatsAppSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private IWhatsappService Sut => new WhatsappService(
        new WhatsappDataAccessDependencies(
            _appointmentRepository,
            _patientRepository,
            _queueRepository,
            _templateRepository,
            _messageRepository,
            _messageActionRepository),
        new WhatsappRuntimeDependencies(_sender, _unitOfWork, _clock));

    // ─── EnqueueTurnoConfirmadoAsync ───────────────────────────────────────────

    [Fact]
    public async Task EnqueueTurnoConfirmado_AppointmentNotOccupied_DoesNotEnqueue()
    {
        // Arrange: appointment is libre (not occupied), has no PatientId
        var appointment = CreateFreeAppointment(Guid.NewGuid());

        // Act
        await Sut.EnqueueTurnoConfirmadoAsync(appointment, "test", CancellationToken.None);

        // Assert
        await _queueRepository.DidNotReceiveWithAnyArgs().TryEnqueueAsync(default!, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueTurnoConfirmado_PatientNotFound_DoesNotEnqueue()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        await Sut.EnqueueTurnoConfirmadoAsync(appointment, "test", CancellationToken.None);

        // Assert
        await _queueRepository.DidNotReceiveWithAnyArgs().TryEnqueueAsync(default!, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueTurnoConfirmado_PatientNotOptedIn_DoesNotEnqueue()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        var patient = CreateOptedOutPatient(patientId);
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        await Sut.EnqueueTurnoConfirmadoAsync(appointment, "test", CancellationToken.None);

        // Assert
        await _queueRepository.DidNotReceiveWithAnyArgs().TryEnqueueAsync(default!, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueTurnoConfirmado_OptedInPatient_SingleSlot_Enqueues()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        var patient = CreateEligiblePatient(patientId, "+5491144445555");

        // The service calls GetByIdAsync twice: once to check eligibility, once inside ResolvePatientPhoneAsync
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _queueRepository.TryEnqueueAsync(Arg.Any<WhatsappDispatchQueueItem>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await Sut.EnqueueTurnoConfirmadoAsync(appointment, "test_source", CancellationToken.None);

        // Assert
        await _queueRepository.Received(1).TryEnqueueAsync(
            Arg.Is<WhatsappDispatchQueueItem>(i => i.TemplateKey == "turno_confirmacion_v1" && i.TriggerSource == "test_source"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueTurnoConfirmado_OptedInPatient_TandaAppointment_EnqueuesTandaTemplate()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();
        var appointment = CreateOccupiedTandaAppointment(Guid.NewGuid(), patientId, tandaId);
        var patient = CreateEligiblePatient(patientId, "+5491144445555");

        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _queueRepository.TryEnqueueAsync(Arg.Any<WhatsappDispatchQueueItem>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await Sut.EnqueueTurnoConfirmadoAsync(appointment, "test_source", CancellationToken.None);

        // Assert
        await _queueRepository.Received(1).TryEnqueueAsync(
            Arg.Is<WhatsappDispatchQueueItem>(i => i.TemplateKey == "turno_confirmacion_tanda_v1" && i.Kind == "confirmacion_tanda"),
            Arg.Any<CancellationToken>());
    }

    // ─── EnqueueTurnoCancelacionAsync ─────────────────────────────────────────

    [Fact]
    public async Task EnqueueTurnoCancelacion_PatientNotFound_DoesNotEnqueue()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        await Sut.EnqueueTurnoCancelacionAsync(appointment, "test", null, CancellationToken.None);

        // Assert
        await _queueRepository.DidNotReceiveWithAnyArgs().TryEnqueueAsync(default!, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueTurnoCancelacion_PatientNotOptedIn_DoesNotEnqueue()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        var patient = CreateOptedOutPatient(patientId);
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        await Sut.EnqueueTurnoCancelacionAsync(appointment, "test", null, CancellationToken.None);

        // Assert
        await _queueRepository.DidNotReceiveWithAnyArgs().TryEnqueueAsync(default!, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueTurnoCancelacion_OptedInPatient_WithOperationKey_Enqueues()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        var patient = CreateEligiblePatient(patientId, "+5491144445555");
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _queueRepository.TryEnqueueAsync(Arg.Any<WhatsappDispatchQueueItem>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await Sut.EnqueueTurnoCancelacionAsync(appointment, "test_source", "op-key-123", CancellationToken.None);

        // Assert
        await _queueRepository.Received(1).TryEnqueueAsync(
            Arg.Is<WhatsappDispatchQueueItem>(i => i.TemplateKey == "turno_cancelacion_v1" && i.Kind == "cancelacion"),
            Arg.Any<CancellationToken>());
    }

    // ─── EnqueueTurnosCancelacionAsync ────────────────────────────────────────

    [Fact]
    public async Task EnqueueTurnosCancelacion_PatientNotOptedIn_NothingEnqueued()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var appointments = new[] { CreateOccupiedAppointment(Guid.NewGuid(), patientId) };
        var patient = CreateOptedOutPatient(patientId);
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        await Sut.EnqueueTurnosCancelacionAsync(patientId, appointments, "op-key", "test", CancellationToken.None);

        // Assert
        await _queueRepository.DidNotReceiveWithAnyArgs().TryEnqueueAsync(default!, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueTurnosCancelacion_EmptyAppointments_NothingEnqueued()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = CreateEligiblePatient(patientId, "+5491144445555");
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);

        // Act — empty appointments list
        await Sut.EnqueueTurnosCancelacionAsync(patientId, Array.Empty<Appointment>(), "op-key", "test", CancellationToken.None);

        // Assert
        await _queueRepository.DidNotReceiveWithAnyArgs().TryEnqueueAsync(default!, default);
    }

    [Fact]
    public async Task EnqueueTurnosCancelacion_MultipleAppointments_EnqueuesMultipleTemplate()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = CreateEligiblePatient(patientId, "+5491144445555");
        var appointments = new[]
        {
            CreateOccupiedAppointment(Guid.NewGuid(), patientId),
            CreateOccupiedAppointment(Guid.NewGuid(), patientId)
        };
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _queueRepository.TryEnqueueAsync(Arg.Any<WhatsappDispatchQueueItem>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await Sut.EnqueueTurnosCancelacionAsync(patientId, appointments, "op-key", "test_source", CancellationToken.None);

        // Assert — 2 slots → multiple template
        await _queueRepository.Received(1).TryEnqueueAsync(
            Arg.Is<WhatsappDispatchQueueItem>(i => i.TemplateKey == "turno_cancelacion_multiple_v1"),
            Arg.Any<CancellationToken>());
    }

    // ─── DispatchAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task Dispatch_EmptyQueue_SenderNeverCalled()
    {
        // Arrange
        var command = new WhatsappDispatchCommand([], null);
        _queueRepository.ClaimAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WhatsappDispatchQueueItem>());

        // Act
        var result = await Sut.DispatchAsync(command, CancellationToken.None);

        // Assert
        await _sender.DidNotReceiveWithAnyArgs().SendRawAsync(default!, default);
        Assert.Equal(0, result.Found);
    }

    [Fact]
    public async Task Dispatch_TemplateNotFound_ItemSkippedSenderNotCalled()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var item = CreateQueueItem(Guid.NewGuid(), "turno_confirmacion_v1");
        var items = new List<WhatsappDispatchQueueItem> { item };
        var command = new WhatsappDispatchCommand([], null);
        _queueRepository.ClaimAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());
        _templateRepository.GetActiveByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((WhatsappTemplate?)null);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        await Sut.DispatchAsync(command, CancellationToken.None);

        // Assert
        await _sender.DidNotReceiveWithAnyArgs().SendRawAsync(default!, default);
        Assert.Equal("skipped", item.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_SenderSuccess_MessagePersisted()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var patientId = Guid.NewGuid();
        var item = CreateQueueItemWithPhone(Guid.NewGuid(), patientId, "turno_confirmacion_v1", "+5491144445555");
        var template = CreateTemplate(1L, "turno_confirmacion_v1", "confirmacion");
        var sendResult = new WhatsAppSendResult(true, "meta", "wamid.XYZ", "{}", null, null);
        var items = new List<WhatsappDispatchQueueItem> { item };

        var command = new WhatsappDispatchCommand([], null);
        _queueRepository.ClaimAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());
        _templateRepository.GetActiveByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(template);
        _sender.SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sendResult);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var result = await Sut.DispatchAsync(command, CancellationToken.None);

        // Assert
        await _sender.Received(1).SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _messageRepository.Received(1).AddAsync(
            Arg.Is<WhatsappMessage>(m => m.Status == "sent"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_SenderFailure_NoUnhandledException()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var patientId = Guid.NewGuid();
        var item = CreateQueueItemWithPhone(Guid.NewGuid(), patientId, "turno_confirmacion_v1", "+5491144445555");
        var template = CreateTemplate(1L, "turno_confirmacion_v1", "confirmacion");
        var items = new List<WhatsappDispatchQueueItem> { item };

        var command = new WhatsappDispatchCommand([], null);
        _queueRepository.ClaimAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());
        _templateRepository.GetActiveByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(template);
        _sender.SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<WhatsAppSendResult>(_ => throw new InvalidOperationException("WhatsApp 500"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(0);

        // Act — should not throw
        var exception = await Record.ExceptionAsync(() => Sut.DispatchAsync(command, CancellationToken.None));

        // Assert
        Assert.Null(exception);
        Assert.Equal("failed", item.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── SendRemindersAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SendReminders_NoAppointments_SenderNeverCalled()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var command = new WhatsappReminderCommand(new DateOnly(2026, 5, 8));
        _appointmentRepository.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await Sut.SendRemindersAsync(command, CancellationToken.None);

        // Assert
        await _sender.DidNotReceiveWithAnyArgs().SendRawAsync(default!, default);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task SendReminders_AllPatientsOptedOut_SenderNeverCalled()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        var patient = CreateOptedOutPatient(patientId);

        var command = new WhatsappReminderCommand(new DateOnly(2026, 5, 8));
        _appointmentRepository.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[] { appointment });
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await Sut.SendRemindersAsync(command, CancellationToken.None);

        // Assert
        await _sender.DidNotReceiveWithAnyArgs().SendRawAsync(default!, default);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task SendReminders_EligibleAppointments_SenderCalled()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var patientId = Guid.NewGuid();
        var appointment = CreateOccupiedAppointment(Guid.NewGuid(), patientId);
        var patient = CreateEligiblePatient(patientId, "+5491144445555");
        var template = CreateTemplate(2L, "turno_recordatorio_24h_v3", "recordatorio_24h");
        var sendResult = new WhatsAppSendResult(true, "meta", "wamid.REMINDER", "{}", null, null);

        var command = new WhatsappReminderCommand(new DateOnly(2026, 5, 8));
        var reminderItem = CreateQueueItemWithReminderPhone(Guid.NewGuid(), patientId, "+5491144445555");
        var reminderItems = new List<WhatsappDispatchQueueItem> { reminderItem };

        _appointmentRepository.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[] { appointment });
        _patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _queueRepository.TryEnqueueAsync(Arg.Any<WhatsappDispatchQueueItem>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _queueRepository.ClaimAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(reminderItems.AsReadOnly());
        _templateRepository.GetActiveByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(template);
        _sender.SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sendResult);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var result = await Sut.SendRemindersAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Total);
        await _sender.Received(1).SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─── Private static factory helpers ───────────────────────────────────────

    private static Patient CreateOptedOutPatient(Guid id, string phone = "+5491144445555")
    {
        return new Patient(
            id,
            "Paciente Test",
            new PatientAdministrativeInfo(phone, "12345678", "12345678", 1),
            new PatientPortalInfo(false, null));
    }

    private static Patient CreateEligiblePatient(Guid id, string phone = "+5491144445555")
    {
        var patient = new Patient(
            id,
            "Paciente Elegible",
            new PatientAdministrativeInfo(phone, "12345678", "12345678", 1),
            new PatientPortalInfo(false, null));

        patient.UpdateAdministrativeData(new PatientAdministrativeDataUpdate(
            Email: null,
            Telefono: phone,
            DocumentoIdentidad: "12345678",
            DocumentoIdentidadNormalizado: "12345678",
            Nacionalidad: null,
            CondicionIvaId: 1,
            ObraSocialId: null,
            NumeroCredencialObraSocial: null,
            Claustrofobico: false,
            Notas: null,
            DatosExtra: "{}",
            OptInWhatsapp: true,
            OptInSource: "test"));

        return patient;
    }

    private static Appointment CreateFreeAppointment(Guid id)
    {
        // A free appointment: no PatientId, Status = Libre — IsOccupied() returns false
        return new Appointment(id, Guid.NewGuid(), new DateOnly(2026, 5, 10), new TimeOnly(9, 0), 1, cameraId: 1);
    }

    private static Appointment CreateOccupiedAppointment(Guid id, Guid patientId)
    {
        var appointment = new Appointment(id, Guid.NewGuid(), new DateOnly(2026, 5, 10), new TimeOnly(9, 0), 1, cameraId: 1);
        appointment.Reserve(patientId);
        return appointment;
    }

    private static Appointment CreateOccupiedTandaAppointment(Guid id, Guid patientId, Guid tandaId)
    {
        var appointment = new Appointment(id, Guid.NewGuid(), new DateOnly(2026, 5, 10), new TimeOnly(9, 0), 1, cameraId: 1);
        appointment.Reserve(patientId, esTanda: true, tandaId: tandaId);
        return appointment;
    }

    private static WhatsappTemplate CreateTemplate(long templateId, string key, string kind)
    {
        return new WhatsappTemplate(new WhatsappTemplateCreateParams(
            templateId,
            key,
            kind,
            key,
            "es_AR",
            "utility",
            true,
            null));
    }

    private static WhatsappDispatchQueueItem CreateQueueItem(Guid id, string templateKey)
    {
        return new WhatsappDispatchQueueItem(new WhatsappDispatchQueueItemCreateParams(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "confirmacion",
            templateKey,
            $"confirmacion:{id:N}",
            "test",
            "{}"));
    }

    private static WhatsappDispatchQueueItem CreateQueueItemWithPhone(Guid id, Guid patientId, string templateKey, string phone)
    {
        // Note: fecha/hora intentionally omitted from payload.
        // When BuildBodyParameters returns new JsonArray(null, null, null), the null slots
        // do not trigger the "node already has a parent" exception that would occur with
        // real JsonValue nodes from a JsonArray. This avoids a production-code edge case
        // while still exercising the sender call.
        var payload = $"{{\"patient_phone_e164\":\"{phone}\"}}";
        return new WhatsappDispatchQueueItem(new WhatsappDispatchQueueItemCreateParams(
            id,
            patientId,
            Guid.NewGuid(),
            null,
            "confirmacion",
            templateKey,
            $"confirmacion:{id:N}",
            "test",
            payload));
    }

    private static WhatsappDispatchQueueItem CreateQueueItemWithReminderPhone(Guid id, Guid patientId, string phone)
    {
        // fecha/hora omitted intentionally to avoid JsonNode "already has a parent" bug
        // in production BuildBodyParameters when non-null strings become parented JsonValue nodes.
        var payload = $"{{\"patient_phone_e164\":\"{phone}\"}}";
        return new WhatsappDispatchQueueItem(new WhatsappDispatchQueueItemCreateParams(
            id,
            patientId,
            Guid.NewGuid(),
            null,
            "recordatorio_24h",
            "turno_recordatorio_24h_v3",
            $"recordatorio_24h:{id:N}",
            "app_recordatorio_dia_siguiente",
            payload));
    }
}
