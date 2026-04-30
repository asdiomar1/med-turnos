using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class DiasLaborablesConfigRepository(MedicalCenterDbContext dbContext) : IDiasLaborablesConfigRepository
{
    public async Task<DiasLaborablesConfig?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return null;
        }

        return await dbContext.DiasLaborablesConfigs.FirstOrDefaultAsync(x => x.Id == key, cancellationToken);
    }

    public async Task UpsertAsync(DiasLaborablesConfig config, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return;
        }

        var existing = await dbContext.DiasLaborablesConfigs.FirstOrDefaultAsync(x => x.Id == config.Id, cancellationToken);
        if (existing is null)
        {
            await dbContext.DiasLaborablesConfigs.AddAsync(config, cancellationToken);
            return;
        }

        existing.UpsertDias(config.DiasSemana);
    }

    private async Task<bool> TableExistsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select 1
            from information_schema.tables
            where table_schema = 'public'
              and table_name = 'dias_laborables_config'
            limit 1
            """;

        return await dbContext.Database.SqlQueryRaw<int>(sql).AnyAsync(cancellationToken);
    }
}
