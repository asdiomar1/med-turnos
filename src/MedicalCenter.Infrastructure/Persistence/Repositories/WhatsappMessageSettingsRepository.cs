using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class WhatsappMessageSettingsRepository(MedicalCenterDbContext dbContext) : IWhatsappMessageSettingsRepository
{
    public async Task<IReadOnlyCollection<WhatsappMessageSetting>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return Array.Empty<WhatsappMessageSetting>();
        }

        return await dbContext.WhatsappMessageSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public async Task<WhatsappMessageSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return null;
        }

        return await dbContext.WhatsappMessageSettings.FirstOrDefaultAsync(x => x.Id == key, cancellationToken);
    }

    public async Task UpsertAsync(WhatsappMessageSetting setting, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return;
        }

        var existing = await dbContext.WhatsappMessageSettings.FirstOrDefaultAsync(x => x.Id == setting.Id, cancellationToken);
        if (existing is null)
        {
            await dbContext.WhatsappMessageSettings.AddAsync(setting, cancellationToken);
            return;
        }

        existing.Update(setting.MessageText, setting.Active);
    }

    private async Task<bool> TableExistsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select 1
            from information_schema.tables
            where table_schema = 'public'
              and table_name = 'whatsapp_message_settings'
            limit 1
            """;

        return await dbContext.Database.SqlQueryRaw<int>(sql).AnyAsync(cancellationToken);
    }
}
