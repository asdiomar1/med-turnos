using MedicalCenter.Domain.Entities;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;

namespace MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating Patient entities in tests.
/// </summary>
public sealed class PatientBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _nombre = "Test Patient";
    private string _dni = "12345678";
    private string _telefono = "+541112345678";
    private string? _documentoIdentidadNormalizado = "12345678";
    private int _condicionIvaId = 1;
    private bool _isActive = true;
    private bool _portalHabilitado;
    private string? _loginIdentifier;
    private string? _email;
    private string? _nacionalidad = "Argentina";
    private int? _obraSocialId;
    private string? _numeroCredencialObraSocial;
    private bool _claustrofobico;
    private string? _notas;
    private string _datosExtra = "{}";
    private bool _optInWhatsapp;
    private string? _optInSource;

    public PatientBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public PatientBuilder WithNombre(string nombre)
    {
        _nombre = nombre;
        return this;
    }

    public PatientBuilder WithDni(string dni)
    {
        _dni = dni;
        _documentoIdentidadNormalizado = dni;
        return this;
    }

    public PatientBuilder WithTelefono(string telefono)
    {
        _telefono = telefono;
        return this;
    }

    public PatientBuilder WithEmail(string? email)
    {
        _email = email;
        return this;
    }

    public PatientBuilder WithObraSocial(int obraSocialId, string? numeroCredencial = null)
    {
        _obraSocialId = obraSocialId;
        _numeroCredencialObraSocial = numeroCredencial;
        return this;
    }

    public PatientBuilder Active()
    {
        _isActive = true;
        return this;
    }

    public PatientBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    public PatientBuilder WithPortalEnabled(string loginIdentifier)
    {
        _portalHabilitado = true;
        _loginIdentifier = loginIdentifier;
        return this;
    }

    public PatientBuilder WithPortalDisabled()
    {
        _portalHabilitado = false;
        _loginIdentifier = null;
        return this;
    }

    public PatientBuilder Claustrofobico(bool claustrofobico = true)
    {
        _claustrofobico = claustrofobico;
        return this;
    }

    public PatientBuilder WithNotas(string? notas)
    {
        _notas = notas;
        return this;
    }

    public PatientBuilder WithOptInWhatsapp(bool optIn = true, string? source = null)
    {
        _optInWhatsapp = optIn;
        _optInSource = source ?? (optIn ? "admin_edicion" : null);
        return this;
    }

    public PatientBuilder WithNacionalidad(string? nacionalidad)
    {
        _nacionalidad = nacionalidad;
        return this;
    }

    public PatientBuilder WithCondicionIva(int condicionIvaId)
    {
        _condicionIvaId = condicionIvaId;
        return this;
    }

    public PatientBuilder WithDatosExtra(string datosExtra)
    {
        _datosExtra = datosExtra;
        return this;
    }

    /// <summary>
    /// Builds the Patient entity with configured values.
    /// </summary>
    public Patient Build()
    {
        var adminInfo = new PatientAdministrativeInfo(
            _telefono,
            _dni,
            _documentoIdentidadNormalizado,
            _condicionIvaId);

        var portalInfo = new PatientPortalInfo(_portalHabilitado, _loginIdentifier);

        var patient = new Patient(_id, _nombre, adminInfo, portalInfo);

        // Set additional properties via reflection
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.Email), _email);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.Nacionalidad), _nacionalidad);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.ObraSocialId), _obraSocialId);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.NumeroCredencialObraSocial), _numeroCredencialObraSocial);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.Claustrofobico), _claustrofobico);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.Notas), _notas);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.DatosExtra), _datosExtra);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.OptInWhatsapp), _optInWhatsapp);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.OptInSource), _optInSource);
        EntityReflectionHelper.SetProperty(patient, nameof(Patient.IsActive), _isActive);

        return patient;
    }

    /// <summary>
    /// Implicit conversion to Patient for cleaner test syntax.
    /// </summary>
    public static implicit operator Patient(PatientBuilder builder) => builder.Build();

    /// <summary>
    /// Creates a default active patient with standard test values.
    /// </summary>
    public static PatientBuilder DefaultActive() => new PatientBuilder().Active();

    /// <summary>
    /// Creates a default inactive patient.
    /// </summary>
    public static PatientBuilder DefaultInactive() => new PatientBuilder().Inactive();
}
