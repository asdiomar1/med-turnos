using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class ProfessionalMappings
{
    public static MedicoSummaryDto ToSummary(this Medico x) =>
        new(x.Id, x.Nombre, x.Activo, x.Orden, x.CreatedAt, x.PerfilId);

    public static ReferenteSummaryDto ToSummary(this Referente x) =>
        new(x.Id, x.Nombre, x.Tipo, x.Activo, x.Orden, x.CreatedAt, x.UpdatedAt);
}