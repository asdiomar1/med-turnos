using MedicalCenter.Application.Abstractions.FeatureFlags;
using MedicalCenter. Infrastructure. Caching;
using Microsoft. Extensions. Configuration;
using Microsoft.Extensions.Logging;

namespace MedicalCenter. Infrastructure. FeatureFlags;

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
        var key = $"feature:{featureName}";

        // Try Redis cache first
        if (_cacheService != null)
        {
            var cached = _cacheService.Get<string>(key);
            if (cached != null)
            {
                return bool.Parse(cached);
            }
        }

        // Fall back to appsettings or environment variable
        var section = _configuration.GetSection("FeatureFlags");
        var featureEnabled = section.GetValue<bool>(featureName, defaultValue);

        // Also check environment variable (highest priority)
        var envValue = Environment.GetEnvironmentVariable($"FeatureFlag__{featureName}");
        if (!string.IsNullOrEmpty(envValue) && bool.TryParse(envValue, out var fromEnv))
        {
            featureEnabled = fromEnv;
        }

        // Cache for 60 seconds (short TTL for runtime toggling)
        _cacheService?.Set(key, featureEnabled.ToString(), TimeSpan.FromSeconds(60));

        _logger.LogDebug("Feature flag '{FeatureName}' = {IsEnabled}", featureName, featureEnabled);
        return featureEnabled;
    }
}