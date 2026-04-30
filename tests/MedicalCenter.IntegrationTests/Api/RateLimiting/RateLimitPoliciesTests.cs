using MedicalCenter.Api.RateLimiting;
using MedicalCenter.Infrastructure.Options;
using System.Threading.RateLimiting;

namespace MedicalCenter.IntegrationTests.Api.RateLimiting;

public sealed class RateLimitPoliciesTests
{
    [Fact]
    public void AuthPolicy_Name_IsAuth()
    {
        Assert.Equal("auth", RateLimitPolicies.Auth);
    }

    [Fact]
    public async Task AuthFixedWindowLimiter_ExceedsPermitLimit_ReturnsRejected()
    {
        var options = new RateLimitingOptions();
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.AuthPermitLimit,
            Window = TimeSpan.FromSeconds(options.AuthWindowSeconds)
        });

        for (int i = 0; i < options.AuthPermitLimit; i++)
        {
            using var lease = await limiter.AcquireAsync();
            Assert.True(lease.IsAcquired);
        }

        using var rejected = await limiter.AcquireAsync();
        Assert.False(rejected.IsAcquired);
    }

    [Fact]
    public async Task WebhookFixedWindowLimiter_WithinLimit_IsAcquired()
    {
        var options = new RateLimitingOptions();
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.WebhookPermitLimit,
            Window = TimeSpan.FromSeconds(options.WebhookWindowSeconds)
        });

        using var lease = await limiter.AcquireAsync();
        Assert.True(lease.IsAcquired);
    }
}
