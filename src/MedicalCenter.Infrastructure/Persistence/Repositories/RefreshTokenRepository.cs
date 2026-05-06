using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(MedicalCenterDbContext dbContext) : IRefreshTokenRepository
{
    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.AddAsync(token, cancellationToken).AsTask();

    public Task<RefreshToken?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.Status == RefreshTokenStatus.Active, cancellationToken);

    public async Task RevokeActiveByUserIdAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.Status == RefreshTokenStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens.Where(token => token.IsActive(now)))
        {
            token.Revoke(now);
        }
    }
}
