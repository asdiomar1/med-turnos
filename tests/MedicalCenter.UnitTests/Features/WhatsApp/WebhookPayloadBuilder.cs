using System.Text.Json;

namespace MedicalCenter.UnitTests.Features.WhatsApp;

internal static class WebhookPayloadBuilder
{
    public static JsonElement BuildStatusPayload(string messageId, string status) =>
        Parse($$"""
        {
          "object": "whatsapp_business_account",
          "entry": [{
            "id": "ACCOUNT_ID",
            "changes": [{
              "field": "messages",
              "value": {
                "messaging_product": "whatsapp",
                "metadata": { "display_phone_number": "5491100000000", "phone_number_id": "PHONE_ID" },
                "statuses": [{
                  "id": "{{messageId}}",
                  "status": "{{status}}",
                  "timestamp": "1700000000",
                  "recipient_id": "5491144445555"
                }]
              }
            }]
          }]
        }
        """);

    public static JsonElement BuildIncomingTextPayload(string from, string body) =>
        Parse($$"""
        {
          "object": "whatsapp_business_account",
          "entry": [{
            "id": "ACCOUNT_ID",
            "changes": [{
              "field": "messages",
              "value": {
                "messaging_product": "whatsapp",
                "metadata": { "display_phone_number": "5491100000000", "phone_number_id": "PHONE_ID" },
                "messages": [{
                  "from": "{{from}}",
                  "id": "wamid.INCOMING",
                  "timestamp": "1700000000",
                  "type": "text",
                  "text": { "body": "{{body}}" }
                }]
              }
            }]
          }]
        }
        """);

    public static JsonElement BuildMalformedPayload() =>
        Parse("""{ "object": "whatsapp_business_account", "entry": "not-an-array" }""");

    public static JsonElement BuildCancellationRequestPayload(string from, Guid slotId, Guid actionId) =>
        Parse($$"""
        {
          "object": "whatsapp_business_account",
          "entry": [{
            "id": "ACCOUNT_ID",
            "changes": [{
              "field": "messages",
              "value": {
                "messaging_product": "whatsapp",
                "messages": [{
                  "from": "{{from}}",
                  "id": "wamid.INCOMING_REQ",
                  "timestamp": "1700000000",
                  "type": "interactive",
                  "interactive": {
                    "type": "button_reply",
                    "button_reply": {
                      "id": "cancelar_turno_solicitar|{{slotId:N}}|{{actionId:N}}",
                      "title": "Cancelar"
                    }
                  }
                }]
              }
            }]
          }]
        }
        """);

    public static JsonElement BuildCancellationDecisionPayload(string from, Guid slotId, Guid actionId, bool confirmar)
    {
        var actionCode = confirmar ? "cancelar_turno_confirmar" : "cancelar_turno_mantener";
        var title = confirmar ? "Cancelar" : "Mantener";
        return Parse($$"""
        {
          "object": "whatsapp_business_account",
          "entry": [{
            "id": "ACCOUNT_ID",
            "changes": [{
              "field": "messages",
              "value": {
                "messaging_product": "whatsapp",
                "messages": [{
                  "from": "{{from}}",
                  "id": "wamid.INCOMING_DEC",
                  "timestamp": "1700000000",
                  "type": "interactive",
                  "interactive": {
                    "type": "button_reply",
                    "button_reply": {
                      "id": "{{actionCode}}|{{slotId:N}}|{{actionId:N}}",
                      "title": "{{title}}"
                    }
                  }
                }]
              }
            }]
          }]
        }
        """);
    }

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();
}
