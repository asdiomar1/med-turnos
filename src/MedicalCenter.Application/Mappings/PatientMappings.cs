using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class PatientMappings
{
    public static PatientSummary ToSummary(this Patient x) =>
        new(
            x.Id,
            x.Nombre,
            x.Email,
            x.Telefono,
            x.DocumentoIdentidad,
            x.DocumentoIdentidadNormalizado,
            x.Nacionalidad,
            x.CondicionIvaId,
            x.ObraSocialId,
            x.NumeroCredencialObraSocial,
            x.PortalHabilitado,
            x.RequiereResetPortal,
            x.LoginIdentifier,
            x.Claustrofobico,
            x.Notas,
            x.DatosExtra,
            x.OptInWhatsapp,
            x.OptInSource);
}