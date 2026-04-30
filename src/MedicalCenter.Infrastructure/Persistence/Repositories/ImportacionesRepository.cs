using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class ImportacionesRepository(MedicalCenterDbContext dbContext) : IImportacionesRepository
{
    public Task<Importacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Importaciones.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(Importacion importacion, CancellationToken cancellationToken) =>
        dbContext.Importaciones.AddAsync(importacion, cancellationToken).AsTask();

    public Task AddErrorsAsync(IEnumerable<ImportacionError> errors, CancellationToken cancellationToken) =>
        dbContext.ImportacionErrors.AddRangeAsync(errors, cancellationToken);

    public async Task<IReadOnlyCollection<ImportacionError>> GetErrorsAsync(Guid importacionId, CancellationToken cancellationToken) =>
        await dbContext.ImportacionErrors
            .AsNoTracking()
            .Where(x => x.ImportacionId == importacionId)
            .OrderBy(x => x.RowNumber)
            .ToListAsync(cancellationToken);
}
