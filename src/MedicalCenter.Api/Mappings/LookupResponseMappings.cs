using System.Text.Json;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.Common;

namespace MedicalCenter.Api.Mappings;

/// <summary>
/// Lookup response mappings — shared across multiple controllers.
/// </summary>
public static class LookupResponseMappings
{
    public static GuidLookupResponse? ToResponse(this GuidLookupSummary? x) =>
        x is null ? null : new()
        {
            Id = x.Id, Nombre = x.Nombre,
            DocumentoIdentidad = x.DocumentoIdentidad, Email = x.Email, Activo = x.Activo,
        };

    public static IntLookupResponse? ToResponse(this IntLookupSummary? x) =>
        x is null ? null : new() { Id = x.Id, Nombre = x.Nombre, Extra = x.Extra, Activo = x.Activo };
}