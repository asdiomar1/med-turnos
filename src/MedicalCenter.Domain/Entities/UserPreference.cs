using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class UserPreference : Entity<Guid>
{
    private UserPreference() { }

    public UserPreference(Guid userId, string? theme, string? customColorsJson, string? turnosLayout, decimal fontScale)
    {
        Id = userId;
        UserId = userId;
        Theme = string.IsNullOrWhiteSpace(theme) ? "system" : theme.Trim();
        CustomColorsJson = NormalizeJson(customColorsJson);
        TurnosLayout = string.IsNullOrWhiteSpace(turnosLayout) ? "standard" : turnosLayout.Trim();
        FontScale = fontScale <= 0 ? 1.0m : fontScale;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid UserId { get; private set; }
    public string Theme { get; private set; } = "system";
    public string? CustomColorsJson { get; private set; }
    public string TurnosLayout { get; private set; } = "standard";
    public decimal FontScale { get; private set; } = 1.0m;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string? theme, string? customColorsJson, string? turnosLayout, decimal? fontScale)
    {
        if (!string.IsNullOrWhiteSpace(theme))
        {
            Theme = theme.Trim();
        }

        CustomColorsJson = NormalizeJson(customColorsJson);

        if (!string.IsNullOrWhiteSpace(turnosLayout))
        {
            TurnosLayout = turnosLayout.Trim();
        }

        if (fontScale.HasValue && fontScale.Value > 0)
        {
            FontScale = fontScale.Value;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim();
    }
}
