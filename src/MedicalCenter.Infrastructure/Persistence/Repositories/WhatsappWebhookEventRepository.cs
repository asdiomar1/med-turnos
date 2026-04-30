using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class WhatsappWebhookEventRepository(MedicalCenterDbContext dbContext) : IWhatsappWebhookEventRepository
{
    public Task AddAsync(WhatsappWebhookEvent webhookEvent, CancellationToken cancellationToken) =>
        dbContext.Set<WhatsappWebhookEvent>().AddAsync(webhookEvent, cancellationToken).AsTask();
}
