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
    public async Task Acquire_AfterWindowResets_AllowsRequestsAgain()
    {
        var limiter = CreateLimiter(permitLimit: 2, windowSeconds: 1);
        limiter.AttemptAcquire().Dispose();
        limiter.AttemptAcquire().Dispose();

        using var rejected = limiter.AttemptAcquire();
        Assert.False(rejected.IsAcquired);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        RateLimitLease? acquired = null;
        while (!cts.Token.IsCancellationRequested)
        {
            acquired = limiter.AttemptAcquire();
            if (acquired.IsAcquired) break;
            acquired.Dispose();
            acquired = null;
            await Task.Delay(50, cts.Token);
        }

        Assert.NotNull(acquired);
        Assert.True(acquired.IsAcquired);
        acquired.Dispose();
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
