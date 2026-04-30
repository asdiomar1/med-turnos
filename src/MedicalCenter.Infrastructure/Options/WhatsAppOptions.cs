namespace MedicalCenter.Infrastructure.Options;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string WebhookVerifyToken { get; init; } = "change-this-webhook-token";
    public string DispatchInternalSecret { get; init; } = "change-this-dispatch-secret";
    public string Provider { get; init; } = "meta";
    public string ProviderFallback { get; init; } = "none";
    public string KapsoBaseUrl { get; init; } = "https://api.kapso.ai/meta/whatsapp";
    public string DefaultLanguageCode { get; init; } = "es_AR";
}
