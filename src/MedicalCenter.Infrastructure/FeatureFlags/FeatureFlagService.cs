using MedicalCenter.Application.Abstractions.FeatureFlags;
using MedicalCenter.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedicalCenter.Infrastructure.FeatureFlags;

/// <summary>
/// Configuration-backed feature flag service with Redis caching
/// for runtime toggle support.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly ICacheService? _cacheService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(
        ICacheService? cacheService,
        IConfiguration configuration,
        ILogger<FeatureFlagService> logger)
    {
        _cacheService = cacheService;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsEnabled(string featureName) => IsEnabled(featureName, defaultValue: false);

    public bool IsEnabled(string featureName, bool defaultValue)
    {
        var cacheKey = $"feature:{featureName}";

        // Try Redis cache first (short TTL for runtime toggling)
        if (_cacheService is not null)
        {
            var cached = _cacheService.GetAsync<string>(cacheKey).GetAwaiter().GetResult();
            if (cached is not null)
            {
                return bool.Parse(cached);
            }
        }

        // Fall back to appsettings
        var section = _configuration.GetSection("FeatureFlags");
        var featureEnabled = section.GetValue<bool>(featureName, defaultValue);

        // Environment variable override (highest priority)
        var envValue = Environment.GetEnvironmentVariable($"FeatureFlag__{featureName}");
        if (!string.IsNullOrEmpty(envValue) && bool.TryParse(envValue, out var fromEnv))
        {
            featureEnabled = fromEnv;
        }

        // Cache for 60 seconds (fire and forget)
        _ = _cacheService?.SetAsync(cacheKey, featureEnabled.ToString(), TimeSpan.FromSeconds(60));

        _logger.LogDebug("Feature flag '{FeatureName}' = {IsEnabled}", featureName, featureEnabled);
        return featureEnabled;
    }
}