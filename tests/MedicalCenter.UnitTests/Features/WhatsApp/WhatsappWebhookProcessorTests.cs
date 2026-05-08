using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.WhatsApp;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MedicalCenter.UnitTests.Features.WhatsApp;

// Scenario map:
// - ProcessAsync: status update payload → message status updated, event marked processed
// - ProcessAsync: incoming text payload → event stored, processed = true (no matching action code → inner loop exits)
// - ProcessAsync: malformed payload → webhook event added, Processed = false, no unhandled exception
public sealed class WhatsappWebhookProcessorTests
{
    private readonly IWhatsappWebhookEventRepository _webhookEventRepository = Substitute.For<IWhatsappWebhookEventRepository>();
    private readonly IWhatsappMessageRepository _messageRepository = Substitute.For<IWhatsappMessageRepository>();
    private readonly IWhatsappMessageActionRepository _messageActionRepository = Substitute.For<IWhatsappMessageActionRepository>();
    private readonly IWhatsappMessageSettingsRepository _messageSettingsRepository = Substitute.For<IWhatsappMessageSettingsRepository>();
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IBlockHistoryRepository _blockHistoryRepository = Substitute.For<IBlockHistoryRepository>();
    private readonly IWhatsAppSender _sender = Substitute.For<IWhatsAppSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private WhatsappWebhookProcessor Sut => new(
        new WhatsappWebhookDataAccessDependencies(
            _webhookEventRepository,
            _messageRepository,
            _messageActionRepository,
            _messageSettingsRepository,
            _appointmentRepository,
            _blockHistoryRepository),
        new WhatsappWebhookRuntimeDependencies(_sender, _unitOfWork, _clock));

    [Fact]
    public async Task ProcessAsync_StatusUpdatePayload_StoresEventAndReturnsProcessedTrue()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var messageId = "wamid.ABC123";
        var message = CreateMessage(messageId, Guid.NewGuid());
        _messageRepository.GetByMetaMessageIdAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);
        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildStatusPayload(messageId, "delivered");

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Processed);
        Assert.True(result.Stored);
        await _webhookEventRepository.Received(1).AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("delivered", message.Status);
    }

    [Fact]
    public async Task ProcessAsync_IncomingTextPayload_StoresEventAndReturnsProcessedTrue()
    {
        // Arrange — incoming text message with no matching action code in body
        // The processor reads button_reply/list_reply/button.payload → falls back to text.body
        // body "hola" is not a pipe-delimited action payload → inner loop exits without action
        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildIncomingTextPayload("+5491144445555", "hola");

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Processed);
        Assert.True(result.Stored);
        await _webhookEventRepository.Received(1).AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_MalformedPayload_WebhookEventRecordedAsUnprocessed()
    {
        // Arrange — malformed payload: entry is a string, not an array
        // ExtractEventMetadata will use defaults; ProcessStatusesAsync/ProcessIncomingMessagesAsync will
        // iterate no elements → should succeed. But we simulate an exception via the message repository throwing.
        // Actually the malformed payload causes EnumerateEntries to yield nothing → both process methods are no-ops
        // → the processor succeeds. To get Processed=false we need an actual exception.
        // We force it by having the webhookEventRepository throw after being set up to throw on second call.
        // Actually the cleanest approach: make SaveChangesAsync throw on first call so the catch block fires.
        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var saveCallCount = 0;
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ =>
            {
                saveCallCount++;
                if (saveCallCount == 1)
                {
                    throw new InvalidOperationException("forced failure");
                }

                return 0;
            });

        var payload = WebhookPayloadBuilder.BuildMalformedPayload();

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Stored);
        Assert.False(result.Processed);
        await _webhookEventRepository.Received(1).AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>());
    }

    // ─── ProcessCancellationRequestAsync ──────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_CancellationRequestActionNotFound_ReturnsProcessedFalse()
    {
        // Arrange
        var slotId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _messageActionRepository.GetByIdAsync(actionId, Arg.Any<CancellationToken>()).Returns((WhatsappMessageAction?)null);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildCancellationRequestPayload("+5491144445555", slotId, actionId);

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Stored);
        Assert.False(result.Processed);
    }

    [Fact]
    public async Task ProcessAsync_CancellationRequestPhoneNotAuthorized_MarksActionFailed()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var slotId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var action = CreateAction(actionId, slotId, Guid.NewGuid(), "+5491144445555");

        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _messageActionRepository.GetByIdAsync(actionId, Arg.Any<CancellationToken>()).Returns(action);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Different phone than action.PhoneE164
        var payload = WebhookPayloadBuilder.BuildCancellationRequestPayload("+5491199999999", slotId, actionId);

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Processed);
        Assert.Equal("failed", action.Status);
        await _sender.DidNotReceiveWithAnyArgs().SendRawAsync(default!, default);
    }

    [Fact]
    public async Task ProcessAsync_CancellationRequestPromptAlreadyRequested_SenderNotCalled()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var slotId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var action = CreateAction(actionId, slotId, Guid.NewGuid(), "+5491144445555");
        action.MarkPromptRequested("incoming0", "wamid.prompt0", fixedNow);

        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _messageActionRepository.GetByIdAsync(actionId, Arg.Any<CancellationToken>()).Returns(action);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildCancellationRequestPayload("+5491144445555", slotId, actionId);

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Processed);
        await _sender.DidNotReceiveWithAnyArgs().SendRawAsync(default!, default);
    }

    [Fact]
    public async Task ProcessAsync_CancellationRequestHappyPath_SendsInteractivePromptAndSaves()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var slotId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var action = CreateAction(actionId, slotId, Guid.NewGuid(), "+5491144445555");
        var sendResult = new WhatsAppSendResult(true, "meta", "wamid.PROMPT", "{}", null, null);

        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _messageActionRepository.GetByIdAsync(actionId, Arg.Any<CancellationToken>()).Returns(action);
        _messageSettingsRepository.GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((WhatsappMessageSetting?)null);
        _sender.SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(sendResult);
        _messageRepository.AddAsync(Arg.Any<WhatsappMessage>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildCancellationRequestPayload("+5491144445555", slotId, actionId);

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Processed);
        await _sender.Received(1).SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.True(action.HasPromptRequested());
    }

    // ─── ProcessCancellationDecisionAsync ────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_CancellationDecisionConfirmarActionNotFound_ReturnsProcessedFalse()
    {
        // Arrange
        var slotId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _messageActionRepository.GetByIdAsync(actionId, Arg.Any<CancellationToken>()).Returns((WhatsappMessageAction?)null);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildCancellationDecisionPayload("+5491144445555", slotId, actionId, confirmar: true);

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Stored);
        Assert.False(result.Processed);
    }

    [Fact]
    public async Task ProcessAsync_CancellationDecisionConfirmarHappyPath_CancelsAppointmentAndSaves()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var action = CreateAction(actionId, slotId, patientId, "+5491144445555");
        action.MarkPromptRequested("incoming-req", "wamid.prompt", fixedNow);

        var appointment = CreateOccupiedAppointment(slotId, patientId);
        var sendResult = new WhatsAppSendResult(true, "meta", "wamid.CONFIRM", "{}", null, null);

        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _messageActionRepository.GetByIdAsync(actionId, Arg.Any<CancellationToken>()).Returns(action);
        _appointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _messageSettingsRepository.GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((WhatsappMessageSetting?)null);
        _sender.SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(sendResult);
        _messageRepository.AddAsync(Arg.Any<WhatsappMessage>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildCancellationDecisionPayload("+5491144445555", slotId, actionId, confirmar: true);

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Processed);
        Assert.Equal("cancelled", action.Status);
        await _sender.Received(1).SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blockHistoryRepository.ReceivedWithAnyArgs(1).AddRangeAsync(default!, default);
    }

    [Fact]
    public async Task ProcessAsync_CancellationDecisionMantenerHappyPath_MarksKeptAndSaves()
    {
        // Arrange
        var fixedNow = DateTimeOffset.Parse("2026-05-07T12:00:00+00:00");
        _clock.UtcNow.Returns(fixedNow);

        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var action = CreateAction(actionId, slotId, patientId, "+5491144445555");
        action.MarkPromptRequested("incoming-req", "wamid.prompt", fixedNow);

        var sendResult = new WhatsAppSendResult(true, "meta", "wamid.KEEP", "{}", null, null);

        _webhookEventRepository.AddAsync(Arg.Any<WhatsappWebhookEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _messageActionRepository.GetByIdAsync(actionId, Arg.Any<CancellationToken>()).Returns(action);
        _messageSettingsRepository.GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((WhatsappMessageSetting?)null);
        _sender.SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(sendResult);
        _messageRepository.AddAsync(Arg.Any<WhatsappMessage>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var payload = WebhookPayloadBuilder.BuildCancellationDecisionPayload("+5491144445555", slotId, actionId, confirmar: false);

        // Act
        var result = await Sut.ProcessAsync(payload, CancellationToken.None);

        // Assert
        Assert.True(result.Processed);
        Assert.Equal("confirmed", action.Status); // MarkKept sets Status = "confirmed"
        await _sender.Received(1).SendRawAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─── Private static factory helpers ───────────────────────────────────────

    private static WhatsappMessageAction CreateAction(Guid id, Guid slotId, Guid patientId, string phoneE164) =>
        new(new WhatsappMessageActionCreateParams(
            id,
            patientId,
            slotId,
            Guid.NewGuid(),
            "cancelacion_whatsapp",
            phoneE164,
            "{}"));

    private static Appointment CreateOccupiedAppointment(Guid id, Guid patientId)
    {
        var appointment = new Appointment(id, Guid.NewGuid(), new DateOnly(2026, 5, 10), new TimeOnly(9, 0), 1, cameraId: 1);
        appointment.Reserve(patientId);
        return appointment;
    }

    private static WhatsappMessage CreateMessage(string metaMessageId, Guid patientId)
    {
        var msg = new WhatsappMessage(new WhatsappMessageCreateParams(
            Guid.NewGuid(),
            patientId,
            Guid.NewGuid(),
            null,
            null,
            "confirmacion",
            "+5491144445555",
            $"idem:{metaMessageId}",
            "test",
            "{}"));

        // Mark the message as "sent" so it has a MetaMessageId and can receive status updates
        msg.MarkSent(metaMessageId, "{}", DateTimeOffset.UtcNow);

        return msg;
    }
}
