using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IWhatsappWebhookEventRepository
{
    Task AddAsync(WhatsappWebhookEvent webhookEvent, CancellationToken cancellationToken);
}
