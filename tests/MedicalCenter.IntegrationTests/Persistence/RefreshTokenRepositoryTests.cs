using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.IntegrationTests.Persistence;

public sealed class RefreshTokenRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RefreshTokenRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RevokeActiveByUserIdAsync_RevokesOnlyActiveTokensForRequestedUser()
    {
        var now = new DateTimeOffset(2026, 5, 5, 21, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var options = CreateOptions();

        await using (var setupContext = new MedicalCenterDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            var activeTarget = new RefreshToken(Guid.NewGuid(), userId, $"target-active-{userId:N}", now.AddHours(1), "jwt-1");
            var expiredTarget = new RefreshToken(Guid.NewGuid(), userId, $"target-expired-{userId:N}", now.AddHours(-1), "jwt-2");
            var otherUserActive = new RefreshToken(Guid.NewGuid(), otherUserId, $"other-active-{otherUserId:N}", now.AddHours(1), "jwt-3");
            var alreadyRevoked = new RefreshToken(Guid.NewGuid(), userId, $"target-revoked-{userId:N}", now.AddHours(1), "jwt-4");
            alreadyRevoked.Revoke(now.AddMinutes(-5));
            var alreadyRotated = new RefreshToken(Guid.NewGuid(), userId, $"target-rotated-{userId:N}", now.AddHours(1), "jwt-5");
            alreadyRotated.Rotate(Guid.NewGuid(), now.AddMinutes(-10));

            await setupContext.RefreshTokens.AddRangeAsync(activeTarget, expiredTarget, otherUserActive, alreadyRevoked, alreadyRotated);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = new MedicalCenterDbContext(options))
        {
            var repository = new RefreshTokenRepository(commandContext);
            await repository.RevokeActiveByUserIdAsync(userId, now, CancellationToken.None);
            await commandContext.SaveChangesAsync();
        }

        await using var verifyContext = new MedicalCenterDbContext(options);
        var tokens = await verifyContext.RefreshTokens
            .Where(token => token.UserId == userId || token.UserId == otherUserId)
            .OrderBy(token => token.TokenHash)
            .ToListAsync();

        Assert.Collection(tokens,
            token =>
            {
                Assert.Equal($"other-active-{otherUserId:N}", token.TokenHash);
                Assert.Equal(RefreshTokenStatus.Active, token.Status);
            },
            token =>
            {
                Assert.Equal($"target-active-{userId:N}", token.TokenHash);
                Assert.Equal(RefreshTokenStatus.Revoked, token.Status);
                Assert.Equal(now, token.RevokedAt);
            },
            token =>
            {
                Assert.Equal($"target-expired-{userId:N}", token.TokenHash);
                Assert.Equal(RefreshTokenStatus.Active, token.Status);
                Assert.Null(token.RevokedAt);
            },
            token =>
            {
                Assert.Equal($"target-revoked-{userId:N}", token.TokenHash);
                Assert.Equal(RefreshTokenStatus.Revoked, token.Status);
            },
            token =>
            {
                Assert.Equal($"target-rotated-{userId:N}", token.TokenHash);
                Assert.Equal(RefreshTokenStatus.Rotated, token.Status);
            });
    }

    private DbContextOptions<MedicalCenterDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;
}
