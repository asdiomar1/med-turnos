using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class PatientRepository(MedicalCenterDbContext dbContext) : IPatientRepository
{
    public async Task<IReadOnlyCollection<Patient>> GetAsync(string? search, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = dbContext.Patients.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Nombre, $"%{normalized}%") ||
                (x.DocumentoIdentidad != null && EF.Functions.ILike(x.DocumentoIdentidad, $"%{normalized}%")) ||
                (x.Email != null && EF.Functions.ILike(x.Email, $"%{normalized}%")));
        }

        return await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    public Task<Patient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken) =>
        dbContext.Patients.FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);

    public Task<Patient?> GetByDocumentoAsync(string documentoIdentidad, CancellationToken cancellationToken)
    {
        var normalized = documentoIdentidad.Trim();
        return dbContext.Patients.FirstOrDefaultAsync(
            x => EF.Functions.ILike(x.DocumentoIdentidad, normalized)
                || (x.DocumentoIdentidadNormalizado != null && EF.Functions.ILike(x.DocumentoIdentidadNormalizado, normalized)),
            cancellationToken);
    }

    public Task<Patient?> GetByLoginIdentifierAsync(string loginIdentifier, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(loginIdentifier)
            ? Task.FromResult<Patient?>(null)
            : dbContext.Patients.FirstOrDefaultAsync(
                x => x.LoginIdentifier != null && EF.Functions.ILike(x.LoginIdentifier, loginIdentifier.Trim()),
                cancellationToken);

    public Task<Patient?> GetByPortalIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return Task.FromResult<Patient?>(null);
        }

        var normalized = identifier.Trim();
        return dbContext.Patients.FirstOrDefaultAsync(
            x =>
                (x.LoginIdentifier != null && EF.Functions.ILike(x.LoginIdentifier, normalized)) ||
                EF.Functions.ILike(x.DocumentoIdentidad, normalized) ||
                (x.DocumentoIdentidadNormalizado != null && EF.Functions.ILike(x.DocumentoIdentidadNormalizado, normalized)) ||
                (x.Email != null && EF.Functions.ILike(x.Email, normalized)),
            cancellationToken);
    }

    public Task AddAsync(Patient patient, CancellationToken cancellationToken) =>
        dbContext.Patients.AddAsync(patient, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<Patient>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return [];
        }

        return await dbContext.Patients
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }
}
