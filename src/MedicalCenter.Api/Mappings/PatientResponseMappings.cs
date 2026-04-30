using System.Text.Json;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.Patients;

namespace MedicalCenter.Api.Mappings;

public static class PatientResponseMappings
{
    public static PatientResponse ToResponse(this PatientSummary x) =>
        new()
        {
            Id = x.Id, Nombre = x.Nombre, Email = x.Email, Telefono = x.Telefono,
            DocumentoIdentidad = x.DocumentoIdentidad, DocumentoIdentidadNormalizado = x.DocumentoIdentidadNormalizado,
            Nacionalidad = x.Nacionalidad, CondicionIvaId = x.CondicionIvaId,
            ObraSocialId = x.ObraSocialId, NumeroCredencialObraSocial = x.NumeroCredencialObraSocial,
            PortalHabilitado = x.PortalHabilitado, RequiereResetPortal = x.RequiereResetPortal,
            LoginIdentifier = x.LoginIdentifier, Claustrofobico = x.Claustrofobico,
            Notas = x.Notas, DatosExtra = JsonSerializer.Deserialize<object>(x.DatosExtra) ?? new { },
            OptInWhatsapp = x.OptInWhatsapp, OptInSource = x.OptInSource,
        };
}