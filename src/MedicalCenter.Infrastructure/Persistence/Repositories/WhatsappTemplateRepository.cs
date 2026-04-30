using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class WhatsappTemplateRepository(MedicalCenterDbContext dbContext) : IWhatsappTemplateRepository
{
    public Task<WhatsappTemplate?> GetActiveByKeyAsync(string key, CancellationToken cancellationToken) =>
        dbContext.WhatsappTemplates.FirstOrDefaultAsync(x => x.Key == key && x.Active, cancellationToken);

    public Task<WhatsappTemplate?> GetActiveByKindAsync(string kind, CancellationToken cancellationToken) =>
        dbContext.WhatsappTemplates.FirstOrDefaultAsync(x => x.Kind == kind && x.Active, cancellationToken);
}
