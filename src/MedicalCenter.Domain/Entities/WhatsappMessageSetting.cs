using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class WhatsappMessageSetting : Entity<string>
{
    private WhatsappMessageSetting() { }

    public WhatsappMessageSetting(
        string key,
        string label,
        string? description,
        string messageText,
        bool active = true)
    {
        Id = key;
        Label = label;
        Description = description;
        MessageText = messageText;
        Active = active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public string Label { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string MessageText { get; private set; } = string.Empty;
    public bool Active { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string messageText, bool active)
    {
        MessageText = messageText;
        Active = active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
