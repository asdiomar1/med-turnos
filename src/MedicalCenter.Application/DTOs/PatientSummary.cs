namespace MedicalCenter.Application.DTOs;

public sealed record PatientSummary(
    Guid Id,
    string Nombre,
    string? Email,
    string Telefono,
    string DocumentoIdentidad,
    string? DocumentoIdentidadNormalizado,
    string? Nacionalidad,
    int CondicionIvaId,
    int? ObraSocialId,
    string? NumeroCredencialObraSocial,
    bool PortalHabilitado,
    bool RequiereResetPortal,
    string? LoginIdentifier,
    bool Claustrofobico,
    string? Notas,
    string DatosExtra,
    bool OptInWhatsapp,
    string? OptInSource);
