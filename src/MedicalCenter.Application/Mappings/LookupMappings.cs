using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class LookupMappings
{
    /// <summary>
    /// Creates a GuidLookupSummary from basic fields without requiring an entity.
    /// Used internally by services that need lookup summaries from partial data.
    /// </summary>
    public static GuidLookupSummary ToGuidLookup(Guid id, string nombre, string? documentoIdentidad = null, string? email = null, bool? activo = null) =>
        new(id, nombre, documentoIdentidad, email, activo);

    /// <summary>
    /// Creates an IntLookupSummary from basic fields without requiring an entity.
    /// </summary>
    public static IntLookupSummary ToIntLookup(int id, string nombre, string? extra = null, bool? activo = null) =>
        new(id, nombre, extra, activo);
}