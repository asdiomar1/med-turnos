using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class WhatsappTemplate : Entity<long>
{
    private WhatsappTemplate() { }

    public WhatsappTemplate(
        long id,
        string key,
        string kind,
        string metaTemplateName,
        string languageCode,
        string category,
        bool active,
        string? description)
    {
        Id = id;
        Key = key;
        Kind = kind;
        MetaTemplateName = metaTemplateName;
        LanguageCode = languageCode;
        Category = category;
        Active = active;
        Description = description;
    }

    public string Key { get; private set; } = string.Empty;
    public string Kind { get; private set; } = string.Empty;
    public string MetaTemplateName { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = "es_AR";
    public string Category { get; private set; } = "utility";
    public bool Active { get; private set; }
    public string? Description { get; private set; }
}
