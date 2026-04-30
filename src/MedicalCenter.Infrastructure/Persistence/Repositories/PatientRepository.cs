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
                x.Nombre.ToLower().Contains(normalized) ||
                (x.DocumentoIdentidad != null && x.DocumentoIdentidad.ToLower().Contains(normalized)) ||
                (x.Email != null && x.Email.ToLower().Contains(normalized)));
        }

        return await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    public Task<Patient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken) =>
        dbContext.Patients.FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);

    public Task<Patient?> GetByDocumentoAsync(string documentoIdentidad, CancellationToken cancellationToken)
    {
        var normalized = documentoIdentidad.Trim().ToLower();
        return dbContext.Patients.FirstOrDefaultAsync(
            x => x.DocumentoIdentidad.ToLower() == normalized || (x.DocumentoIdentidadNormalizado != null && x.DocumentoIdentidadNormalizado.ToLower() == normalized),
            cancellationToken);
    }

    public Task<Patient?> GetByLoginIdentifierAsync(string loginIdentifier, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(loginIdentifier)
            ? Task.FromResult<Patient?>(null)
            : dbContext.Patients.FirstOrDefaultAsync(
                x => x.LoginIdentifier != null && x.LoginIdentifier.ToLower() == loginIdentifier.Trim().ToLower(),
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
                (x.LoginIdentifier != null && x.LoginIdentifier.ToLower() == normalized) ||
                x.DocumentoIdentidad.ToLower() == normalized ||
                (x.DocumentoIdentidadNormalizado != null && x.DocumentoIdentidadNormalizado.ToLower() == normalized) ||
                (x.Email != null && x.Email.ToLower() == normalized),
            cancellationToken);
    }

    public Task AddAsync(Patient patient, CancellationToken cancellationToken) =>
        dbContext.Patients.AddAsync(patient, cancellationToken).AsTask();
}
