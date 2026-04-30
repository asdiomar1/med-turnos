using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class ClinicalHistoryRepository(MedicalCenterDbContext dbContext) : IClinicalHistoryRepository
{
    public Task<ClinicalHistory?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken) =>
        dbContext.ClinicalHistories.FirstOrDefaultAsync(x => x.PatientId == patientId, cancellationToken);

    public async Task<IReadOnlyCollection<ClinicalHistoryNumeroSummary>> GetResumenAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ClinicalHistories
                .AsNoTracking()
                .OrderBy(x => x.PatientId)
                .Select(x => new ClinicalHistoryNumeroSummary(x.PatientId, x.Numero))
                .ToListAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return [];
        }
    }

    public async Task<IReadOnlyCollection<ClinicalEvolution>> GetEvolutionsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken) =>
        await dbContext.ClinicalEvolutions
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.FechaClinica)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(ClinicalHistory history, CancellationToken cancellationToken) =>
        dbContext.ClinicalHistories.AddAsync(history, cancellationToken).AsTask();

    public Task AddEvolutionAsync(ClinicalEvolution evolution, CancellationToken cancellationToken) =>
        dbContext.ClinicalEvolutions.AddAsync(evolution, cancellationToken).AsTask();

    public async Task<long> GetNextNumeroAsync(CancellationToken cancellationToken)
    {
        var max = await dbContext.ClinicalHistories
            .AsNoTracking()
            .MaxAsync(x => (long?)x.Numero, cancellationToken);
        return (max ?? 0) + 1;
    }

    public Task<bool> IsNumeroTakenAsync(long numero, Guid excludePatientId, CancellationToken cancellationToken) =>
        dbContext.ClinicalHistories
            .AsNoTracking()
            .AnyAsync(x => x.Numero == numero && x.PatientId != excludePatientId, cancellationToken);
}
