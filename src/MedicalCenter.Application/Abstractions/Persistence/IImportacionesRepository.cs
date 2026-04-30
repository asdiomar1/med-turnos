using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IImportacionesRepository
{
    Task<Importacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Importacion importacion, CancellationToken cancellationToken);
    Task AddErrorsAsync(IEnumerable<ImportacionError> errors, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ImportacionError>> GetErrorsAsync(Guid importacionId, CancellationToken cancellationToken);
}
