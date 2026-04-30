using System.Text.Json;
using System.Text.Json.Nodes;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.WhatsApp;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.WhatsApp;

public sealed class WhatsappWebhookProcessor(
    IWhatsappWebhookEventRepository webhookEventRepository,
    IWhatsappMessageRepository messageRepository,
    IWhatsappMessageActionRepository messageActionRepository,
    IWhatsappMessageSettingsRepository messageSettingsRepository,
    IAppointmentRepository appointmentRepository,
    IBlockHistoryRepository blockHistoryRepository,
    IWhatsAppSender sender,
    IUnitOfWork unitOfWork,
    IClock clock) : IWhatsappWebhookProcessor
{
    public async Task<WhatsappWebhookProcessingResult> ProcessAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var rawPayload = payload.GetRawText();
        var metaObject = GetStringProperty(payload, "object") ?? "unknown";
        var (eventType, entryId, messageId) = ExtractEventMetadata(payload);

        var webhookEvent = new WhatsappWebhookEvent(
            eventType,
            metaObject,
            entryId,
            messageId,
            rawPayload,
            processed: false);

        await webhookEventRepository.AddAsync(webhookEvent, cancellationToken);

        try
        {
            await ProcessStatusesAsync(payload, cancellationToken);
            await ProcessIncomingMessagesAsync(payload, cancellationToken);
            webhookEvent.MarkProcessed();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new WhatsappWebhookProcessingResult(true, true, eventType, entryId, messageId);
        }
        catch (Exception exception)
        {
            webhookEvent.MarkFailed(exception.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new WhatsappWebhookProcessingResult(true, false, eventType, entryId, messageId);
        }
    }

    private async Task ProcessStatusesAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        foreach (var status in EnumerateStatuses(payload))
        {
            var metaMessageId = GetStringProperty(status, "id");
            if (string.IsNullOrWhiteSpace(metaMessageId))
            {
                continue;
            }

            var message = await messageRepository.GetByMetaMessageIdAsync(metaMessageId, cancellationToken);
            if (message is null)
            {
                continue;
            }

            var statusValue = GetStringProperty(status, "status")?.ToLowerInvariant();
            var now = clock.UtcNow;
            switch (statusValue)
            {
                case "delivered":
                    message.MarkDelivered(now);
                    break;
                case "read":
                    message.MarkRead(now);
                    break;
                case "failed":
                    message.MarkFailed(
                        GetStringProperty(GetObjectProperty(status, "error"), "code") ?? "failed",
                        GetStringProperty(GetObjectProperty(status, "error"), "message") ?? "Webhook informó fallo.",
                        message.ResponsePayload ?? "{}",
                        now);
                    break;
            }
        }
    }

    private async Task ProcessIncomingMessagesAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        foreach (var message in EnumerateIncomingMessages(payload))
        {
            var fromPhone = NormalizePhoneToE164(GetStringProperty(message, "from"));
            var replyPayload = ExtractReplyPayload(message);
            if (string.IsNullOrWhiteSpace(replyPayload))
            {
                continue;
            }

            var parts = replyPayload.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            var actionCode = parts[0].Trim();
            if (!Guid.TryParse(parts[1], out var slotId) || !Guid.TryParse(parts[2], out var actionId))
            {
                continue;
            }

            var incomingMessageId = GetStringProperty(message, "id");

            switch (actionCode.ToLowerInvariant())
            {
                case "cancelar_turno_solicitar":
                    await ProcessCancellationRequestAsync(slotId, actionId, fromPhone, incomingMessageId, cancellationToken);
                    break;
                case "cancelar_turno_confirmar":
                    await ProcessCancellationDecisionAsync(slotId, actionId, fromPhone, incomingMessageId, shouldCancel: true, cancellationToken);
                    break;
                case "cancelar_turno_mantener":
                    await ProcessCancellationDecisionAsync(slotId, actionId, fromPhone, incomingMessageId, shouldCancel: false, cancellationToken);
                    break;
            }
        }
    }

    private async Task ProcessCancellationRequestAsync(Guid slotId, Guid actionId, string? fromPhone, string? incomingMessageId, CancellationToken cancellationToken)
    {
        var action = await messageActionRepository.GetByIdAsync(actionId, cancellationToken);
        if (action is null)
        {
            throw new ValidationException("Acción de WhatsApp no encontrada.");
        }

        var normalizedPhone = NormalizePhoneToE164(fromPhone);
        if (normalizedPhone is null || !string.Equals(action.PhoneE164, normalizedPhone, StringComparison.OrdinalIgnoreCase))
        {
            action.MarkFailed("Telefono no autorizado para esta acción.", clock.UtcNow);
            return;
        }

        if (string.Equals(action.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            await SendStatusMessageAsync(action, "cancelacion_turno_ya_cancelado", cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.Equals(action.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
        {
            await SendStatusMessageAsync(action, "cancelacion_accion_ya_resuelta", cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (action.HasPromptRequested())
        {
            return;
        }

        var bodyText = await ResolveMessageTextAsync("cancelacion_confirmacion_solicitud", cancellationToken);
        var promptPayload = BuildInteractiveButtonsPayload(
            action.PhoneE164,
            bodyText,
            $"cancelar_turno_confirmar|{slotId:N}|{actionId:N}",
            $"cancelar_turno_mantener|{slotId:N}|{actionId:N}");

        var sendResult = await sender.SendRawAsync(promptPayload.ToJsonString(), cancellationToken);
        var outgoingMessage = new WhatsappMessage(
            Guid.NewGuid(),
            action.PatientId,
            action.SlotId,
            null,
            null,
            "cancelacion_whatsapp",
            action.PhoneE164,
            $"cancelacion_prompt:{actionId:N}",
            "whatsapp_webhook",
            promptPayload.ToJsonString());

        if (!sendResult.Ok)
        {
            outgoingMessage.MarkFailed(
                sendResult.ErrorCode ?? "send_failed",
                sendResult.ErrorMessage ?? "No se pudo enviar la confirmación de cancelación.",
                sendResult.ResponsePayloadJson ?? "{}",
                clock.UtcNow);
            action.MarkFailed(sendResult.ErrorMessage ?? "No se pudo enviar la confirmación de cancelación.", clock.UtcNow);
            await messageRepository.AddAsync(outgoingMessage, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        outgoingMessage.MarkSent(sendResult.ProviderMessageId, sendResult.ResponsePayloadJson ?? "{}", clock.UtcNow);
        await messageRepository.AddAsync(outgoingMessage, cancellationToken);
        action.MarkPromptRequested(incomingMessageId, sendResult.ProviderMessageId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessCancellationDecisionAsync(Guid slotId, Guid actionId, string? fromPhone, string? incomingMessageId, bool shouldCancel, CancellationToken cancellationToken)
    {
        var action = await messageActionRepository.GetByIdAsync(actionId, cancellationToken);
        if (action is null)
        {
            throw new ValidationException("Acción de WhatsApp no encontrada.");
        }

        var normalizedPhone = NormalizePhoneToE164(fromPhone);
        if (normalizedPhone is null || !string.Equals(action.PhoneE164, normalizedPhone, StringComparison.OrdinalIgnoreCase))
        {
            action.MarkFailed("Telefono no autorizado para esta acción.", clock.UtcNow);
            return;
        }

        if (string.Equals(action.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            await SendStatusMessageAsync(action, "cancelacion_turno_ya_cancelado", cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.Equals(action.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
        {
            await SendStatusMessageAsync(action, "cancelacion_accion_ya_resuelta", cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!action.HasPromptRequested())
        {
            await ProcessCancellationRequestAsync(slotId, actionId, fromPhone, incomingMessageId, cancellationToken);
            return;
        }

        if (shouldCancel)
        {
            var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken);
            if (appointment is null)
            {
                await SendStatusMessageAsync(action, "cancelacion_rechazada", cancellationToken);
                action.MarkFailed("Turno no encontrado.", clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            if (!appointment.IsOccupied() || appointment.PatientId != action.PatientId)
            {
                await SendStatusMessageAsync(action, "cancelacion_rechazada", cancellationToken);
                action.MarkRejected(incomingMessageId, clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            try
            {
                appointment.Cancel("Cancelado por WhatsApp");
            }
            catch (InvalidOperationException exception)
            {
                await SendStatusMessageAsync(action, "cancelacion_rechazada", cancellationToken);
                action.MarkFailed(exception.Message, clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            await blockHistoryRepository.AddRangeAsync(
            [
                new BlockHistory(
                    Guid.NewGuid(),
                    appointment.Fecha,
                    appointment.Hora,
                    appointment.CameraId,
                    appointment.Id,
                    appointment.Lugar,
                    "cancelado_por_whatsapp",
                    action.PatientId,
                    null,
                    "Paciente cancelo por WhatsApp",
                    appointment.ReferidoTercero,
                    appointment.ModalidadCobro,
                    appointment.ObraSocialId,
                    appointment.NumeroAutorizacion,
                    null,
                    null,
                    appointment.MedicoId,
                    appointment.EsNuevoIngreso,
                    appointment.ReferenteId,
                    appointment.TandaId,
                    appointment.SesionesAutorizadas,
                    appointment.CicloObraSocialId)
            ], cancellationToken);

            await SendStatusMessageAsync(action, "cancelacion_confirmada", cancellationToken);
            action.MarkCancelled(incomingMessageId, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        await SendStatusMessageAsync(action, "cancelacion_mantener_confirmado", cancellationToken);
        action.MarkKept(incomingMessageId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SendStatusMessageAsync(WhatsappMessageAction action, string settingKey, CancellationToken cancellationToken)
    {
        var messageText = await ResolveMessageTextAsync(settingKey, cancellationToken);
        var payload = BuildTextPayload(action.PhoneE164, messageText);
        var requestJson = payload.ToJsonString();
        var sendResult = await sender.SendRawAsync(requestJson, cancellationToken);

        var outgoingMessage = new WhatsappMessage(
            Guid.NewGuid(),
            action.PatientId,
            action.SlotId,
            null,
            null,
            "cancelacion_whatsapp",
            action.PhoneE164,
            $"{settingKey}:{action.Id:N}",
            "whatsapp_webhook",
            requestJson);

        if (sendResult.Ok)
        {
            outgoingMessage.MarkSent(sendResult.ProviderMessageId, sendResult.ResponsePayloadJson ?? "{}", clock.UtcNow);
        }
        else
        {
            outgoingMessage.MarkFailed(
                sendResult.ErrorCode ?? "send_failed",
                sendResult.ErrorMessage ?? $"No se pudo enviar el mensaje {settingKey}.",
                sendResult.ResponsePayloadJson ?? "{}",
                clock.UtcNow);
        }

        await messageRepository.AddAsync(outgoingMessage, cancellationToken);
    }

    private async Task<string> ResolveMessageTextAsync(string key, CancellationToken cancellationToken)
    {
        var setting = await messageSettingsRepository.GetByKeyAsync(key, cancellationToken);
        if (setting is not null && setting.Active && !string.IsNullOrWhiteSpace(setting.MessageText))
        {
            return setting.MessageText;
        }

        return key switch
        {
            "cancelacion_confirmacion_solicitud" => "Confirma si querés cancelar tu turno del {{fecha}} a las {{hora}}.",
            "cancelacion_confirmada" => "Tu turno fue cancelado correctamente. Si necesitas reprogramar, comunicate con el centro.",
            "cancelacion_mantener_confirmado" => "Perfecto. Tu turno sigue confirmado.",
            "cancelacion_turno_ya_cancelado" => "Ese turno ya fue cancelado anteriormente.",
            "cancelacion_accion_ya_resuelta" => "Ya registramos que decidiste mantener este turno. Si querés cancelarlo, respondenos nuevamente desde el recordatorio.",
            "cancelacion_rechazada" => "No pudimos cancelar el turno porque ya no estaba disponible o cambió su estado. Si necesitás ayuda, comunicate con el centro.",
            _ => string.Empty
        };
    }

    private static JsonObject BuildTextPayload(string phone, string body) =>
        new()
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = phone,
            ["type"] = "text",
            ["text"] = new JsonObject
            {
                ["preview_url"] = false,
                ["body"] = body
            }
        };

    private static JsonObject BuildInteractiveButtonsPayload(string phone, string body, string cancelPayload, string keepPayload) =>
        new()
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = phone,
            ["type"] = "interactive",
            ["interactive"] = new JsonObject
            {
                ["type"] = "button",
                ["body"] = new JsonObject
                {
                    ["text"] = body
                },
                ["action"] = new JsonObject
                {
                    ["buttons"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "reply",
                            ["reply"] = new JsonObject
                            {
                                ["id"] = cancelPayload,
                                ["title"] = "Cancelar"
                            }
                        },
                        new JsonObject
                        {
                            ["type"] = "reply",
                            ["reply"] = new JsonObject
                            {
                                ["id"] = keepPayload,
                                ["title"] = "Mantener"
                            }
                        }
                    }
                }
            }
        };

    private static IEnumerable<JsonElement> EnumerateStatuses(JsonElement payload)
    {
        foreach (var entry in EnumerateEntries(payload))
        {
            foreach (var change in EnumerateArray(entry, "changes"))
            {
                var value = GetObjectProperty(change, "value");
                foreach (var status in EnumerateArray(value, "statuses"))
                {
                    yield return status;
                }
            }
        }
    }

    private static IEnumerable<JsonElement> EnumerateIncomingMessages(JsonElement payload)
    {
        foreach (var entry in EnumerateEntries(payload))
        {
            foreach (var change in EnumerateArray(entry, "changes"))
            {
                var value = GetObjectProperty(change, "value");
                foreach (var message in EnumerateArray(value, "messages"))
                {
                    yield return message;
                }
            }
        }
    }

    private static string? ExtractReplyPayload(JsonElement message)
    {
        var interactive = GetObjectProperty(message, "interactive");
        var buttonReply = GetObjectProperty(interactive, "button_reply");
        var listReply = GetObjectProperty(interactive, "list_reply");

        return GetStringProperty(buttonReply, "id")
               ?? GetStringProperty(listReply, "id")
               ?? GetStringProperty(GetObjectProperty(message, "button"), "payload")
               ?? GetStringProperty(GetObjectProperty(message, "button"), "text")
               ?? GetStringProperty(GetObjectProperty(message, "text"), "body");
    }

    private static IEnumerable<JsonElement> EnumerateEntries(JsonElement payload) =>
        EnumerateArray(payload, "entry");

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in property.EnumerateArray())
        {
            yield return item;
        }
    }

    private static (string eventType, string? entryId, string? messageId) ExtractEventMetadata(JsonElement payload)
    {
        var entry = EnumerateEntries(payload).FirstOrDefault();
        var change = EnumerateArray(entry, "changes").FirstOrDefault();
        var value = GetObjectProperty(change, "value");

        var eventType = GetStringProperty(change, "field")
            ?? GetStringProperty(value, "event_type")
            ?? GetStringProperty(payload, "event_type")
            ?? "webhook";

        var entryId = GetStringProperty(entry, "id");
        var messageId = GetStringProperty(EnumerateArray(value, "messages").FirstOrDefault(), "id")
            ?? GetStringProperty(EnumerateArray(value, "statuses").FirstOrDefault(), "id");

        return (eventType, entryId, messageId);
    }

    private static JsonElement GetObjectProperty(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? property
            : default;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static string? NormalizePhoneToE164(string? value)
    {
        var raw = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        if (raw.StartsWith('+') && digits.Length >= 10)
        {
            return $"+{digits}";
        }

        if (digits.StartsWith("549") && digits.Length >= 12)
        {
            return $"+{digits}";
        }

        if (digits.StartsWith("54") && digits.Length >= 11)
        {
            return $"+549{digits[2..]}";
        }

        var localDigits = digits.TrimStart('0');
        if (localDigits.Length is >= 10 and <= 11)
        {
            return $"+549{localDigits}";
        }

        return null;
    }
}
