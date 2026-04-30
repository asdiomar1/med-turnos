using System.Threading.RateLimiting;
using MedicalCenter.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MedicalCenter.UnitTests.RateLimiting;

public sealed class FixedWindowRateLimiterTests
{
    [Fact]
    public void Acquire_WithinPermitLimit_AllowsRequests()
    {
        var limiter = CreateLimiter(permitLimit: 5, windowSeconds: 60);

        for (int i = 0; i < 5; i++)
        {
            using var lease = limiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);
        }
    }

    [Fact]
    public void Acquire_ExceedsPermitLimit_RejectsRequest()
    {
        var limiter = CreateLimiter(permitLimit: 5, windowSeconds: 60);

        for (int i = 0; i < 5; i++)
        {
            limiter.AttemptAcquire().Dispose();
        }

        using var lease = limiter.AttemptAcquire();
        Assert.False(lease.IsAcquired);
    }

    [Fact]
    public void Acquire_AfterWindowResets_AllowsRequestsAgain()
    {
        var limiter = CreateLimiter(permitLimit: 2, windowSeconds: 1);
        limiter.AttemptAcquire().Dispose();
        limiter.AttemptAcquire().Dispose();

        using var rejected = limiter.AttemptAcquire();
        Assert.False(rejected.IsAcquired);

        Thread.Sleep(1100);

        using var lease = limiter.AttemptAcquire();
        Assert.True(lease.IsAcquired);
    }

    private static FixedWindowRateLimiter CreateLimiter(int permitLimit, int windowSeconds)
    {
        return new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    }
}
