using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed record WhatsappTemplateCreateParams(
    long Id,
    string Key,
    string Kind,
    string MetaTemplateName,
    string LanguageCode,
    string Category,
    bool Active,
    string? Description);

public sealed class WhatsappTemplate : Entity<long>
{
    private WhatsappTemplate() { }

    public WhatsappTemplate(WhatsappTemplateCreateParams p)
    {
        Id = p.Id;
        Key = p.Key;
        Kind = p.Kind;
        MetaTemplateName = p.MetaTemplateName;
        LanguageCode = p.LanguageCode;
        Category = p.Category;
        Active = p.Active;
        Description = p.Description;
    }

    public string Key { get; private set; } = string.Empty;
    public string Kind { get; private set; } = string.Empty;
    public string MetaTemplateName { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = "es_AR";
    public string Category { get; private set; } = "utility";
    public bool Active { get; private set; }
    public string? Description { get; private set; }
}
