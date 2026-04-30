using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IPortalAccessTokenRepository
{
    Task AddAsync(PortalAccessToken token, CancellationToken cancellationToken);
    Task<PortalAccessToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}
