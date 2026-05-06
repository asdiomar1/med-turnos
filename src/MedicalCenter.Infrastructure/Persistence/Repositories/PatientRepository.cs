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
            var normalized = search.Trim().ToLower();
            query = query.Where(x =>
                x.Nombre.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (x.DocumentoIdentidad != null && x.DocumentoIdentidad.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (x.Email != null && x.Email.Contains(normalized, StringComparison.OrdinalIgnoreCase)));
        }

        return await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    public Task<Patient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken) =>
        dbContext.Patients.FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);

    public Task<Patient?> GetByDocumentoAsync(string documentoIdentidad, CancellationToken cancellationToken)
    {
        var normalized = documentoIdentidad.Trim().ToLower();
        return dbContext.Patients.FirstOrDefaultAsync(
            x => string.Equals(x.DocumentoIdentidad, normalized, StringComparison.OrdinalIgnoreCase)
                || (x.DocumentoIdentidadNormalizado != null && string.Equals(x.DocumentoIdentidadNormalizado, normalized, StringComparison.OrdinalIgnoreCase)),
            cancellationToken);
    }

    public Task<Patient?> GetByLoginIdentifierAsync(string loginIdentifier, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(loginIdentifier)
            ? Task.FromResult<Patient?>(null)
            : dbContext.Patients.FirstOrDefaultAsync(
                x => x.LoginIdentifier != null && string.Equals(x.LoginIdentifier, loginIdentifier.Trim(), StringComparison.OrdinalIgnoreCase),
                cancellationToken);

    public Task<Patient?> GetByPortalIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return Task.FromResult<Patient?>(null);
        }

        var normalized = identifier.Trim().ToLower();
        return dbContext.Patients.FirstOrDefaultAsync(
            x =>
                (x.LoginIdentifier != null && string.Equals(x.LoginIdentifier, normalized, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(x.DocumentoIdentidad, normalized, StringComparison.OrdinalIgnoreCase) ||
                (x.DocumentoIdentidadNormalizado != null && string.Equals(x.DocumentoIdentidadNormalizado, normalized, StringComparison.OrdinalIgnoreCase)) ||
                (x.Email != null && string.Equals(x.Email, normalized, StringComparison.OrdinalIgnoreCase)),
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
