using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class CatalogMappings
{
    public static CondicionIvaSummaryDto ToSummary(this CondicionIva x) =>
        new(x.Id, x.Nombre, x.Activo, x.Orden, x.CreatedAt);

    public static ObraSocialSummaryDto ToSummary(this ObraSocial x) =>
        new(x.Id, x.Nombre, x.Activa, x.TieneConvenio, x.Orden, x.Abreviatura, x.CreatedAt);
}