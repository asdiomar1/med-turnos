using System.Text.Json;
using System.Text.Json.Nodes;
using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class WhatsappMessageAction : Entity<Guid>
{
    private WhatsappMessageAction() { }

    public WhatsappMessageAction(
        Guid id,
        Guid patientId,
        Guid slotId,
        Guid whatsappMessageId,
        string actionKind,
        string phoneE164,
        string rawContext,
        string status = "pending_confirmation")
    {
        Id = id;
        PatientId = patientId;
        SlotId = slotId;
        WhatsappMessageId = whatsappMessageId;
        ActionKind = actionKind;
        PhoneE164 = phoneE164;
        RawContext = rawContext;
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid PatientId { get; private set; }
    public Guid SlotId { get; private set; }
    public Guid WhatsappMessageId { get; private set; }
    public string ActionKind { get; private set; } = string.Empty;
    public string Status { get; private set; } = "pending_confirmation";
    public string PhoneE164 { get; private set; } = string.Empty;
    public string? IncomingMessageId { get; private set; }
    public string? ConfirmedIncomingMessageId { get; private set; }
    public string RawContext { get; private set; } = "{}";
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkConfirmed(string? incomingMessageId, DateTimeOffset now)
    {
        Status = "confirmed";
        IncomingMessageId = incomingMessageId;
        UpdatedAt = now;
    }

    public void MarkCompleted(string? confirmedIncomingMessageId, DateTimeOffset now)
    {
        Status = "completed";
        ConfirmedIncomingMessageId = confirmedIncomingMessageId;
        UpdatedAt = now;
    }

    public void MarkRejected(string? incomingMessageId, DateTimeOffset now)
    {
        Status = "rejected";
        IncomingMessageId = incomingMessageId;
        UpdatedAt = now;
    }

    public void MarkFailed(string errorMessage, DateTimeOffset now)
    {
        Status = "failed";
        ErrorMessage = errorMessage;
        UpdatedAt = now;
    }

    public void MarkPromptRequested(string? incomingMessageId, string? promptMessageId, DateTimeOffset now)
    {
        IncomingMessageId = incomingMessageId;
        UpdateRawContext(context =>
        {
            context["prompt_requested"] = true;
            if (!string.IsNullOrWhiteSpace(promptMessageId))
            {
                context["prompt_message_id"] = promptMessageId;
            }
            context["prompt_requested_at"] = now;
        });

        UpdatedAt = now;
    }

    public void MarkCancelled(string? incomingMessageId, DateTimeOffset now)
    {
        Status = "cancelled";
        ConfirmedIncomingMessageId = incomingMessageId;
        ErrorMessage = null;
        UpdatedAt = now;
    }

    public void MarkKept(string? incomingMessageId, DateTimeOffset now)
    {
        Status = "confirmed";
        ConfirmedIncomingMessageId = incomingMessageId;
        ErrorMessage = null;
        UpdatedAt = now;
    }

    public bool HasPromptRequested()
    {
        var context = ParseRawContext();
        return context.TryGetProperty("prompt_requested", out var value) && value.ValueKind is JsonValueKind.True;
    }

    public string? GetPromptMessageId()
    {
        var context = ParseRawContext();
        return context.TryGetProperty("prompt_message_id", out var value) ? value.GetString() : null;
    }

    private void UpdateRawContext(Action<JsonObject> update)
    {
        var context = ParseRawContextObject();
        update(context);
        RawContext = context.ToJsonString();
    }

    private JsonObject ParseRawContextObject()
    {
        try
        {
            var parsed = JsonNode.Parse(string.IsNullOrWhiteSpace(RawContext) ? "{}" : RawContext);
            return parsed as JsonObject ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private JsonElement ParseRawContext()
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(RawContext) ? "{}" : RawContext);
            return document.RootElement.Clone();
        }
        catch
        {
            return default;
        }
    }
}
