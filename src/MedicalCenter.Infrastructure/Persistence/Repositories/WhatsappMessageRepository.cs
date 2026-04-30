using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class WhatsappMessageRepository(MedicalCenterDbContext dbContext) : IWhatsappMessageRepository
{
    public Task AddAsync(WhatsappMessage message, CancellationToken cancellationToken) =>
        dbContext.WhatsappMessages.AddAsync(message, cancellationToken).AsTask();

    public Task<WhatsappMessage?> GetByMetaMessageIdAsync(string metaMessageId, CancellationToken cancellationToken) =>
        dbContext.WhatsappMessages.FirstOrDefaultAsync(x => x.MetaMessageId == metaMessageId, cancellationToken);
}
