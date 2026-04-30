using MedicalCenter.Application.DTOs;
using System.Text.Json;

namespace MedicalCenter.Application.Features.WhatsApp;

public interface IWhatsappWebhookProcessor
{
    Task<WhatsappWebhookProcessingResult> ProcessAsync(JsonElement payload, CancellationToken cancellationToken);
}
