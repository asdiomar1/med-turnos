namespace MedicalCenter.Application.Abstractions.WhatsApp;

public interface IWhatsAppSender
{
    Task<WhatsAppSendResult> SendRawAsync(string requestPayloadJson, CancellationToken cancellationToken);
}

public sealed record WhatsAppSendResult(
    bool Ok,
    string Provider,
    string? ProviderMessageId,
    string? ResponsePayloadJson,
    string? ErrorCode,
    string? ErrorMessage);
