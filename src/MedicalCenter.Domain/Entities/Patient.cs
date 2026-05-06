using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Patient : Entity<Guid>
{
    private Patient() { }

    public Patient(
        Guid id,
        string nombre,
        PatientAdministrativeInfo administrativeInfo,
        PatientPortalInfo portalInfo)
    {
        Id = id;
        Nombre = nombre;
        Telefono = administrativeInfo.Telefono;
        DocumentoIdentidad = administrativeInfo.DocumentoIdentidad;
        DocumentoIdentidadNormalizado = administrativeInfo.DocumentoIdentidadNormalizado;
        CondicionIvaId = administrativeInfo.CondicionIvaId;
        PortalHabilitado = portalInfo.PortalHabilitado;
        LoginIdentifier = portalInfo.LoginIdentifier;
        IsActive = true;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string Telefono { get; private set; } = string.Empty;
    public string DocumentoIdentidad { get; private set; } = string.Empty;
    public string? DocumentoIdentidadNormalizado { get; private set; }
    public string? Nacionalidad { get; private set; }
    public int CondicionIvaId { get; private set; }
    public int? ObraSocialId { get; private set; }
    public string? NumeroCredencialObraSocial { get; private set; }
    public bool PortalHabilitado { get; private set; }
    public bool RequiereResetPortal { get; private set; }
    public string? LoginIdentifier { get; private set; }
    public bool Claustrofobico { get; private set; }
    public string? Notas { get; private set; }
    public string DatosExtra { get; private set; } = "{}";
    public bool OptInWhatsapp { get; private set; }
    public string? OptInSource { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateAdministrativeData(PatientAdministrativeDataUpdate update)
    {
        Email = update.Email;
        Telefono = update.Telefono;
        DocumentoIdentidad = update.DocumentoIdentidad;
        DocumentoIdentidadNormalizado = update.DocumentoIdentidadNormalizado;
        Nacionalidad = update.Nacionalidad;
        CondicionIvaId = update.CondicionIvaId;
        ObraSocialId = update.ObraSocialId;
        NumeroCredencialObraSocial = update.NumeroCredencialObraSocial;
        Claustrofobico = update.Claustrofobico;
        Notas = update.Notas;
        DatosExtra = update.DatosExtra;
        OptInWhatsapp = update.OptInWhatsapp;
        OptInSource = ResolveOptInSource(update.OptInWhatsapp, update.OptInSource);
    }

    public void UpdateOwnData(string nombre, string? email, string telefono)
    {
        Nombre = nombre;
        Email = email;
        Telefono = telefono;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void ConfigurePortal(bool portalHabilitado, bool requiereResetPortal)
    {
        PortalHabilitado = portalHabilitado;
        RequiereResetPortal = requiereResetPortal;
    }

    public void SetLoginIdentifier(string loginIdentifier)
    {
        LoginIdentifier = loginIdentifier;
    }

    public void MarkResetRequired()
    {
        RequiereResetPortal = true;
    }

    private static string? ResolveOptInSource(bool optInWhatsapp, string? optInSource)
    {
        if (!optInWhatsapp)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(optInSource) ? "admin_edicion" : optInSource.Trim();
    }
}

public sealed record PatientAdministrativeInfo(
    string Telefono,
    string DocumentoIdentidad,
    string? DocumentoIdentidadNormalizado,
    int CondicionIvaId);

public sealed record PatientPortalInfo(bool PortalHabilitado, string? LoginIdentifier = null);

public sealed record PatientAdministrativeDataUpdate(
    string? Email,
    string Telefono,
    string DocumentoIdentidad,
    string? DocumentoIdentidadNormalizado,
    string? Nacionalidad,
    int CondicionIvaId,
    int? ObraSocialId,
    string? NumeroCredencialObraSocial,
    bool Claustrofobico,
    string? Notas,
    string DatosExtra,
    bool OptInWhatsapp,
    string? OptInSource);
