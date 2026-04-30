using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Patient : Entity<Guid>
{
    private Patient() { }

    public Patient(
        Guid id,
        string nombre,
        string telefono,
        string documentoIdentidad,
        string? documentoIdentidadNormalizado,
        int condicionIvaId,
        bool portalHabilitado,
        string? loginIdentifier = null)
    {
        Id = id;
        Nombre = nombre;
        Telefono = telefono;
        DocumentoIdentidad = documentoIdentidad;
        DocumentoIdentidadNormalizado = documentoIdentidadNormalizado;
        CondicionIvaId = condicionIvaId;
        PortalHabilitado = portalHabilitado;
        LoginIdentifier = loginIdentifier;
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

    public void UpdateAdministrativeData(
        string? email,
        string telefono,
        string documentoIdentidad,
        string? documentoIdentidadNormalizado,
        string? nacionalidad,
        int condicionIvaId,
        int? obraSocialId,
        string? numeroCredencialObraSocial,
        bool claustrofobico,
        string? notas,
        string datosExtra,
        bool optInWhatsapp,
        string? optInSource)
    {
        Email = email;
        Telefono = telefono;
        DocumentoIdentidad = documentoIdentidad;
        DocumentoIdentidadNormalizado = documentoIdentidadNormalizado;
        Nacionalidad = nacionalidad;
        CondicionIvaId = condicionIvaId;
        ObraSocialId = obraSocialId;
        NumeroCredencialObraSocial = numeroCredencialObraSocial;
        Claustrofobico = claustrofobico;
        Notas = notas;
        DatosExtra = datosExtra;
        OptInWhatsapp = optInWhatsapp;
        OptInSource = optInWhatsapp
            ? (string.IsNullOrWhiteSpace(optInSource) ? "admin_edicion" : optInSource.Trim())
            : null;
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
}
