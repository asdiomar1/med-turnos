using System.Text;
using System.Text.Json;
using MedicalCenter.Application.Abstractions.WhatsApp;
using MedicalCenter.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MedicalCenter.Infrastructure.WhatsApp;

public sealed class WhatsAppSender(HttpClient httpClient, IOptions<WhatsAppOptions> options) : IWhatsAppSender
{
    private const string KapsoProvider = "kapso";
    private const string MetaProvider = "meta";
    public async Task<WhatsAppSendResult> SendRawAsync(string requestPayloadJson, CancellationToken cancellationToken)
    {
        var provider = NormalizeProvider(options.Value.Provider);
        var primary = await SendWithProviderAsync(provider, requestPayloadJson, cancellationToken);
        if (primary.Ok || provider != KapsoProvider || NormalizeProvider(options.Value.ProviderFallback) != MetaProvider)
        {
            return primary;
        }

        var fallback = await SendWithProviderAsync(MetaProvider, requestPayloadJson, cancellationToken);
        if (!fallback.Ok)
        {
            return new WhatsAppSendResult(
                false,
                KapsoProvider,
                primary.ProviderMessageId,
                fallback.ResponsePayloadJson,
                fallback.ErrorCode ?? primary.ErrorCode,
                fallback.ErrorMessage ?? primary.ErrorMessage);
        }

        return fallback with { Provider = MetaProvider };
    }

    private async Task<WhatsAppSendResult> SendWithProviderAsync(string provider, string requestPayloadJson, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint(provider));
            request.Headers.Accept.ParseAdd("application/json");
            request.Content = new StringContent(requestPayloadJson, Encoding.UTF8, "application/json");
            AddAuthenticationHeaders(request, provider);

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responsePayloadJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var providerMessageId = ExtractMessageId(responsePayloadJson);

            if (!response.IsSuccessStatusCode)
            {
                return new WhatsAppSendResult(
                    false,
                    provider,
                    providerMessageId,
                    responsePayloadJson,
                    ExtractErrorCode(responsePayloadJson, (int)response.StatusCode),
                    ExtractErrorMessage(responsePayloadJson, provider));
            }

            return new WhatsAppSendResult(true, provider, providerMessageId, responsePayloadJson, null, null);
        }
        catch (Exception exception)
        {
            return new WhatsAppSendResult(false, provider, null, null, "fetch_error", exception.Message);
        }
    }

    private static string NormalizeProvider(string? value) =>
        string.Equals(value?.Trim(), KapsoProvider, StringComparison.OrdinalIgnoreCase) ? KapsoProvider : MetaProvider;

    private static string BuildEndpoint(string provider)
    {
        var phoneNumberId = Environment.GetEnvironmentVariable("WHATSAPP_PHONE_NUMBER_ID") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(phoneNumberId))
        {
            throw new InvalidOperationException("Falta WHATSAPP_PHONE_NUMBER_ID.");
        }

        return provider == KapsoProvider
            ? $"{(Environment.GetEnvironmentVariable("KAPSO_BASE_URL") ?? "https://api.kapso.ai/meta/whatsapp").TrimEnd('/')}/v24.0/{phoneNumberId}/messages"
            : $"https://graph.facebook.com/v23.0/{phoneNumberId}/messages";
    }

    private static void AddAuthenticationHeaders(HttpRequestMessage request, string provider)
    {
        if (provider == KapsoProvider)
        {
            var apiKey = Environment.GetEnvironmentVariable("KAPSO_API_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Falta KAPSO_API_KEY.");
            }

            request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
            return;
        }

        var accessToken = Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Falta WHATSAPP_ACCESS_TOKEN.");
        }

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static string? ExtractMessageId(string responsePayloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responsePayloadJson);
            var root = document.RootElement;
            if (root.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
            {
                var first = messages.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("id", out var id))
                {
                    return id.GetString();
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static string? ExtractErrorCode(string responsePayloadJson, int fallbackCode)
    {
        try
        {
            using var document = JsonDocument.Parse(responsePayloadJson);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object && error.TryGetProperty("code", out var code))
            {
                return code.ValueKind == JsonValueKind.Number ? code.GetInt32().ToString() : code.ToString();
            }
        }
        catch
        {
            // ignored
        }

        return fallbackCode.ToString();
    }

    private static string? ExtractErrorMessage(string responsePayloadJson, string provider)
    {
        try
        {
            using var document = JsonDocument.Parse(responsePayloadJson);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch
        {
            // ignored
        }

        return $"No se pudo enviar el mensaje por {provider}";
    }
}
