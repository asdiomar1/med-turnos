using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class WhatsappMessageActionRepository(MedicalCenterDbContext dbContext) : IWhatsappMessageActionRepository
{
    public Task AddAsync(WhatsappMessageAction action, CancellationToken cancellationToken) =>
        dbContext.WhatsappMessageActions.AddAsync(action, cancellationToken).AsTask();

    public Task<WhatsappMessageAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.WhatsappMessageActions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
