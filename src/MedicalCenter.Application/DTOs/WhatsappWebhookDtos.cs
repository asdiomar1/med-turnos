using System.Text.Json;

namespace MedicalCenter.Application.DTOs;

public sealed record WhatsappWebhookProcessingResult(
    bool Stored,
    bool Processed,
    string EventType,
    string? EntryId,
    string? MessageId);

public sealed record WhatsappWebhookEventInput(
    JsonElement Payload,
    string? Signature);
