using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class PortalAccessTokenRepository(MedicalCenterDbContext dbContext) : IPortalAccessTokenRepository
{
    public Task AddAsync(PortalAccessToken token, CancellationToken cancellationToken) =>
        dbContext.PortalAccessTokens.AddAsync(token, cancellationToken).AsTask();

    public Task<PortalAccessToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.PortalAccessTokens.FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash && x.UsedAt == null && x.RevokedAt == null,
            cancellationToken);
}
