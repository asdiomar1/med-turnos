using System.Text.Json;
using System.Text.Json.Nodes;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.WhatsApp;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Constants;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.WhatsApp;

public sealed class WhatsappService(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IWhatsappDispatchQueueRepository queueRepository,
    IWhatsappTemplateRepository templateRepository,
    IWhatsappMessageRepository messageRepository,
    IWhatsappMessageActionRepository messageActionRepository,
    IWhatsAppSender sender,
    IBlockHistoryRepository blockHistoryRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IWhatsappService
{
    public async Task<WhatsappDispatchResult> DispatchAsync(WhatsappDispatchCommand command, CancellationToken cancellationToken)
    {
        var slotIds = command.SlotIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (command.Limit.HasValue && command.Limit.Value > 0)
        {
            slotIds = slotIds.Take(command.Limit.Value).ToArray();
        }

        var items = await queueRepository.ClaimAsync(command.Limit ?? 25, slotIds, cancellationToken);
        foreach (var item in items)
        {
            await ProcessQueueItemAsync(item, cancellationToken);
        }

        return new WhatsappDispatchResult(slotIds.Length, items.Count);
    }

    public async Task<WhatsappReminderResult> SendRemindersAsync(WhatsappReminderCommand command, CancellationToken cancellationToken)
    {
        var fechaObjetivo = command.FechaObjetivo ?? GetTomorrowDateInBuenosAires(clock.UtcNow.UtcDateTime);
        var appointments = await appointmentRepository.GetByDateAsync(fechaObjetivo, cancellationToken);

        var candidates = new List<ReminderCandidate>();
        foreach (var appointment in appointments.Where(x => x.Status == AppointmentStatus.Ocupado && x.PatientId.HasValue))
        {
            var patient = await patientRepository.GetByIdAsync(appointment.PatientId!.Value, cancellationToken);
            if (!IsEligibleForWhatsapp(patient))
            {
                continue;
            }

            candidates.Add(new ReminderCandidate(appointment, patient!));
        }

        foreach (var candidate in candidates)
        {
            var queued = await EnqueueReminderAsync(candidate, fechaObjetivo, cancellationToken);
            if (!queued)
            {
                continue;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var reminderSlotIds = candidates.Select(x => x.Appointment.Id).ToArray();
        if (reminderSlotIds.Length > 0)
        {
            var queuedItems = await queueRepository.ClaimAsync(Math.Max(reminderSlotIds.Length + 10, 25), reminderSlotIds, cancellationToken);
            foreach (var item in queuedItems)
            {
                await ProcessQueueItemAsync(item, cancellationToken);
            }
        }

        return new WhatsappReminderResult(fechaObjetivo, candidates.Count);
    }

    public async Task EnqueueTurnoConfirmadoAsync(Appointment appointment, string triggerSource, CancellationToken cancellationToken)
    {
        if (!appointment.IsOccupied() || !appointment.PatientId.HasValue)
        {
            return;
        }

        var patient = await patientRepository.GetByIdAsync(appointment.PatientId.Value, cancellationToken);
        if (!IsEligibleForWhatsapp(patient))
        {
            return;
        }

        if (appointment.TandaId.HasValue)
        {
            await EnqueueConfirmationAsync(
                patient!.Id,
                appointment.Id,
                appointment.TandaId,
                "confirmacion_tanda",
                "turno_confirmacion_tanda_v1",
                $"confirmacion_tanda:{appointment.TandaId.Value:N}",
                triggerSource,
                new JsonObject
                {
                    ["primer_slot_id"] = appointment.Id.ToString(),
                    ["fecha"] = appointment.Fecha.ToString("yyyy-MM-dd"),
                    ["hora"] = appointment.Hora.ToString("HH:mm")
                },
                cancellationToken);
            return;
        }

        await EnqueueConfirmationAsync(
            patient!.Id,
            appointment.Id,
            null,
            "confirmacion",
            "turno_confirmacion_v1",
            $"confirmacion:{appointment.Id:N}:{appointment.PatientId:N}:{appointment.UpdatedAt:O}",
            triggerSource,
            new JsonObject
            {
                ["fecha"] = appointment.Fecha.ToString("yyyy-MM-dd"),
                ["hora"] = appointment.Hora.ToString("HH:mm"),
                ["camara_id"] = appointment.CameraId
            },
            cancellationToken);
    }

    public async Task EnqueueTurnoCancelacionAsync(Appointment appointment, string triggerSource, string? operationKey, CancellationToken cancellationToken)
    {
        if (!appointment.PatientId.HasValue)
        {
            return;
        }

        var patient = await patientRepository.GetByIdAsync(appointment.PatientId.Value, cancellationToken);
        if (!IsEligibleForWhatsapp(patient))
        {
            return;
        }

        await EnqueueCancellationSingleAsync(
            patient!.Id,
            appointment,
            triggerSource,
            operationKey,
            cancellationToken);
    }

    public async Task EnqueueTurnosCancelacionAsync(Guid patientId, IReadOnlyCollection<Appointment> appointments, string operationKey, string triggerSource, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken);
        if (!IsEligibleForWhatsapp(patient))
        {
            return;
        }

        var canceledSlots = appointments
            .Where(x => x.Id != Guid.Empty)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .ToArray();

        if (canceledSlots.Length == 0)
        {
            return;
        }

        if (canceledSlots.Length == 1)
        {
            await EnqueueCancellationSingleAsync(patient!.Id, canceledSlots[0], triggerSource, operationKey, cancellationToken);
            return;
        }

        var representative = canceledSlots[0];
        var payload = new JsonObject
        {
            ["operation_key"] = string.IsNullOrWhiteSpace(operationKey) ? "cancelacion_multiple" : operationKey.Trim(),
            ["cancelled_slots"] = new JsonArray(canceledSlots.Select(slot => new JsonObject
            {
                ["slot_id"] = slot.Id.ToString(),
                ["fecha"] = slot.Fecha.ToString("yyyy-MM-dd"),
                ["hora"] = slot.Hora.ToString("HH:mm")
            }).ToArray())
        };

        await TryEnqueueAsync(
            new WhatsappDispatchQueueItem(
                Guid.NewGuid(),
                patient!.Id,
                representative.Id,
                representative.TandaId,
                "cancelacion",
                "turno_cancelacion_multiple_v1",
                $"cancelacion_multiple:{operationKey}:{patient.Id}",
                triggerSource,
                payload.ToJsonString()),
            cancellationToken);
    }

    private async Task<bool> EnqueueReminderAsync(ReminderCandidate candidate, DateOnly fechaObjetivo, CancellationToken cancellationToken)
    {
        var phone = NormalizePhoneToE164(candidate.Patient.Telefono);
        if (phone is null)
        {
            return false;
        }

        var idempotencyKey = $"recordatorio_24h:{candidate.Appointment.Id}:{fechaObjetivo:yyyyMMdd}";
        var payload = new JsonObject
        {
            ["fecha"] = candidate.Appointment.Fecha.ToString("yyyy-MM-dd"),
            ["hora"] = candidate.Appointment.Hora.ToString("HH:mm"),
            ["fecha_objetivo"] = fechaObjetivo.ToString("yyyy-MM-dd"),
            ["hora_envio_argentina"] = WhatsAppConstants.DefaultArgentinaSendHour,
            ["patient_phone_e164"] = phone,
        }!.ToJsonString();

        return await queueRepository.TryEnqueueAsync(new WhatsappDispatchQueueItem(
            Guid.NewGuid(),
            candidate.Patient.Id,
            candidate.Appointment.Id,
            candidate.Appointment.TandaId,
            "recordatorio_24h",
            "turno_recordatorio_24h_v3",
            idempotencyKey,
            "app_recordatorio_dia_siguiente",
            payload), cancellationToken);
    }

    private async Task EnqueueCancellationSingleAsync(Guid patientId, Appointment appointment, string triggerSource, string? operationKey, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["cancelled_slots"] = new JsonArray
            {
                new JsonObject
                {
                    ["slot_id"] = appointment.Id.ToString(),
                    ["fecha"] = appointment.Fecha.ToString("yyyy-MM-dd"),
                    ["hora"] = appointment.Hora.ToString("HH:mm")
                }
            }
        };

        await TryEnqueueAsync(
            new WhatsappDispatchQueueItem(
                Guid.NewGuid(),
                patientId,
                appointment.Id,
                appointment.TandaId,
                "cancelacion",
                "turno_cancelacion_v1",
                string.IsNullOrWhiteSpace(operationKey)
                    ? $"cancelacion:{appointment.Id:N}:{patientId:N}:{appointment.UpdatedAt:O}"
                    : $"cancelacion:{operationKey.Trim()}:{appointment.Id:N}:{patientId:N}",
                triggerSource,
                payload.ToJsonString()),
            cancellationToken);
    }

    private async Task EnqueueConfirmationAsync(
        Guid patientId,
        Guid slotId,
        Guid? tandaId,
        string kind,
        string templateKey,
        string idempotencyKey,
        string triggerSource,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        var phone = await ResolvePatientPhoneAsync(patientId, cancellationToken);
        if (phone is null)
        {
            return;
        }

        payload["patient_phone_e164"] = phone;

        await TryEnqueueAsync(
            new WhatsappDispatchQueueItem(
                Guid.NewGuid(),
                patientId,
                slotId,
                tandaId,
                kind,
                templateKey,
                idempotencyKey,
                triggerSource,
                payload.ToJsonString()),
            cancellationToken);
    }

    private async Task<bool> TryEnqueueAsync(WhatsappDispatchQueueItem item, CancellationToken cancellationToken)
    {
        var enqueued = await queueRepository.TryEnqueueAsync(item, cancellationToken);
        return enqueued;
    }

    private async Task ProcessQueueItemAsync(WhatsappDispatchQueueItem item, CancellationToken cancellationToken)
    {
        try
        {
            var template = await templateRepository.GetActiveByKeyAsync(item.TemplateKey, cancellationToken);
            if (template is null)
            {
                item.MarkSkipped($"Template no disponible: {item.TemplateKey}", clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var payload = JsonNode.Parse(item.Payload)?.AsObject() ?? new JsonObject();
            var phone = NormalizePhoneToE164(payload["patient_phone_e164"]?.ToString()) ?? await ResolvePatientPhoneAsync(item.PatientId, cancellationToken);
            if (phone is null)
            {
                item.MarkSkipped("Paciente sin telefono valido.", clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            Guid? reminderActionId = null;
            if (string.Equals(item.Kind, "recordatorio_24h", StringComparison.OrdinalIgnoreCase) && item.SlotId.HasValue)
            {
                reminderActionId = Guid.NewGuid();
                payload["action_id"] = reminderActionId.Value.ToString();
            }

            var requestPayload = BuildRequestPayload(template, item, payload, phone);
            var requestJson = requestPayload.ToJsonString();
            var sendResult = await sender.SendRawAsync(requestJson, cancellationToken);

            var whatsappMessage = new WhatsappMessage(
                Guid.NewGuid(),
                item.PatientId,
                item.SlotId,
                item.TandaId,
                template.Id,
                item.Kind,
                phone,
                item.IdempotencyKey,
                item.TriggerSource,
                requestJson);

            if (sendResult.Ok)
            {
                whatsappMessage.MarkSent(sendResult.ProviderMessageId, sendResult.ResponsePayloadJson ?? "{}", clock.UtcNow);
                await messageRepository.AddAsync(whatsappMessage, cancellationToken);

                if (string.Equals(item.Kind, "recordatorio_24h", StringComparison.OrdinalIgnoreCase) && item.SlotId.HasValue)
                {
                    var actionId = reminderActionId ?? Guid.NewGuid();
                    var actionPayload = BuildReminderActionContext(item, phone, actionId).ToJsonString();
                    var action = new WhatsappMessageAction(
                        actionId,
                        item.PatientId,
                        item.SlotId.Value,
                        whatsappMessage.Id,
                        "cancelar_turno",
                        phone,
                        actionPayload);
                    await messageActionRepository.AddAsync(action, cancellationToken);
                }

                item.MarkProcessed(clock.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            whatsappMessage.MarkFailed(sendResult.ErrorCode ?? "send_failed", sendResult.ErrorMessage ?? "No se pudo enviar el mensaje", sendResult.ResponsePayloadJson ?? "{}", clock.UtcNow);
            await messageRepository.AddAsync(whatsappMessage, cancellationToken);
            item.MarkFailed(sendResult.ErrorMessage ?? "No se pudo enviar el mensaje", clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            item.MarkFailed(exception.Message, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private JsonObject BuildRequestPayload(WhatsappTemplate template, WhatsappDispatchQueueItem item, JsonObject payload, string phone)
    {
        var bodyParameters = BuildBodyParameters(item.Kind, payload);
        var request = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = phone,
            ["type"] = "template",
            ["template"] = new JsonObject
            {
                ["name"] = template.MetaTemplateName,
                ["language"] = new JsonObject { ["code"] = template.LanguageCode },
            }
        };

        var components = new JsonArray();
        if (bodyParameters.Count > 0)
        {
            var bodyComponent = new JsonObject
            {
                ["type"] = "body",
                ["parameters"] = new JsonArray(bodyParameters.Select(value => new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = value,
                }).ToArray())
            };
            components.Add(bodyComponent);
        }

        if (string.Equals(item.Kind, "recordatorio_24h", StringComparison.OrdinalIgnoreCase) && item.SlotId.HasValue)
        {
            var actionPayload = $"cancelar_turno_solicitar|{item.SlotId.Value:N}|{payload["action_id"]?.ToString() ?? string.Empty}";
            components.Add(new JsonObject
            {
                ["type"] = "button",
                ["sub_type"] = "quick_reply",
                ["index"] = "0",
                ["parameters"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "payload",
                        ["payload"] = actionPayload
                    }
                }
            });
        }

        ((JsonObject)request["template"]!).Add("components", components);
        return request;
    }

    private static JsonArray BuildBodyParameters(string kind, JsonObject payload)
    {
        string? fecha = payload["fecha"]?.ToString();
        string? hora = payload["hora"]?.ToString();

        return kind.ToLowerInvariant() switch
        {
            "confirmacion" => new JsonArray(fecha, hora, payload["camara_id"]?.ToString()),
            "confirmacion_tanda" => new JsonArray(fecha, hora, payload["primer_slot_id"]?.ToString()),
            "cancelacion" => new JsonArray(
                payload["cancelled_slots"] is JsonArray cancelled && cancelled.Count > 0
                    ? cancelled[0]?["fecha"]?.ToString()
                    : fecha,
                payload["cancelled_slots"] is JsonArray cancelled2 && cancelled2.Count > 0
                    ? cancelled2[0]?["hora"]?.ToString()
                    : hora),
            "recordatorio_24h" => new JsonArray(fecha, hora),
            _ => new JsonArray(fecha, hora)
        };
    }

    private static JsonObject BuildReminderActionContext(WhatsappDispatchQueueItem item, string phone, Guid actionId) =>
        new()
        {
            ["slot_id"] = item.SlotId?.ToString(),
            ["patient_id"] = item.PatientId.ToString(),
            ["phone_e164"] = phone,
            ["action_id"] = actionId.ToString(),
            ["kind"] = item.Kind,
        };

    private async Task<string?> ResolvePatientPhoneAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken);
        return IsEligibleForWhatsapp(patient) ? NormalizePhoneToE164(patient!.Telefono) : null;
    }

    private async Task RegisterCancellationHistoryAsync(WhatsappMessageAction action, Appointment appointment, CancellationToken cancellationToken)
    {
        var history = new BlockHistory(
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
            appointment.CicloObraSocialId);

        await blockHistoryRepository.AddRangeAsync([history], cancellationToken);
    }

    private static bool IsEligibleForWhatsapp(Patient? patient) =>
        patient is not null &&
        patient.IsActive &&
        patient.OptInWhatsapp &&
        !string.IsNullOrWhiteSpace(patient.Telefono);

    private static DateOnly GetTomorrowDateInBuenosAires(DateTime utcNow)
    {
        var timeZone = GetArgentinaTimeZone();
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        return DateOnly.FromDateTime(local.Date.AddDays(1));
    }

    private static TimeZoneInfo GetArgentinaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
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

    private static string[] GetCancellationActionPayloadParts(string payload)
    {
        var parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 3 ? parts : [];
    }

    private sealed record ReminderCandidate(Appointment Appointment, Patient Patient);
}
