namespace MedicalCenter.Application.Abstractions.FeatureFlags;

/// <summary>
/// Service for managing feature flags.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Check if a feature is enabled.
    /// </summary>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Check if a feature is enabled with a default value.
    /// </summary>
    bool IsEnabled(string featureName, bool defaultValue);
}

/// <summary>
/// Feature flag names.
/// </summary>
public static class FeatureFlags
{
    /// <summary>Enable enhanced appointment notifications.</summary>
    public const string EnhancedNotifications = "enhanced_notifications";

    /// <summary>Enable WhatsApp message sending.</summary>
    public const string WhatsAppEnabled = "whatsapp_enabled";

    /// <summary>Enable export functionality.</summary>
    public const string ExportEnabled = "export_enabled";

    /// <summary>Enable patient import functionality.</summary>
    public const string ImportEnabled = "import_enabled";

    /// <summary>Enable Redis distributed caching.</summary>
    public const string RedisCachingEnabled = "redis_caching_enabled";
}