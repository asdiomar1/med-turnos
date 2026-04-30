using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface ICondicionIvaRepository
{
    Task<IReadOnlyCollection<CondicionIva>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
}
