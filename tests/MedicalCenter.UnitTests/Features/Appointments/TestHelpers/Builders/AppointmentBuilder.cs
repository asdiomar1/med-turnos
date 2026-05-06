using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;

namespace MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating Appointment entities in tests.
/// </summary>
public sealed class AppointmentBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _scheduleId = Guid.NewGuid();
    private DateOnly _fecha = new(2026, 5, 2);
    private TimeOnly _hora = new(9, 0);
    private int _lugar = 1;
    private int? _cameraId = 1;
    private AppointmentStatus _status = AppointmentStatus.Libre;
    private Guid? _patientId;
    private Guid? _blockId;
    private Guid? _tandaId;
    private bool _esTanda;
    private bool _esBloqueCompleto;
    private bool _referidoTercero;
    private int? _referenteId;
    private string _modalidadCobro = "particular";
    private int? _obraSocialId;
    private string? _numeroAutorizacion;
    private int? _sesionesAutorizadas;
    private Guid? _cicloObraSocialId;
    private bool _iniciarNuevoCicloObraSocial;
    private bool _convenioCorroborado;
    private int? _medicoId;
    private Guid? _medicoUserId;
    private bool _esNuevoIngreso;
    private bool _esMonoxido;
    private bool _monoxidoOrdenMedica;
    private bool _monoxidoResumenClinico;
    private string? _notes;
    private Guid? _apartadoPorUserId;
    private DateTimeOffset? _apartadoTs;
    private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;

    public AppointmentBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AppointmentBuilder WithScheduleId(Guid scheduleId)
    {
        _scheduleId = scheduleId;
        return this;
    }

    public AppointmentBuilder WithFecha(DateOnly fecha)
    {
        _fecha = fecha;
        return this;
    }

    public AppointmentBuilder WithFecha(int year, int month, int day)
    {
        _fecha = new DateOnly(year, month, day);
        return this;
    }

    public AppointmentBuilder WithHora(TimeOnly hora)
    {
        _hora = hora;
        return this;
    }

    public AppointmentBuilder WithHora(int hour, int minute)
    {
        _hora = new TimeOnly(hour, minute);
        return this;
    }

    public AppointmentBuilder WithLugar(int lugar)
    {
        _lugar = lugar;
        return this;
    }

    public AppointmentBuilder WithCameraId(int? cameraId)
    {
        _cameraId = cameraId;
        return this;
    }

    public AppointmentBuilder WithStatus(AppointmentStatus status)
    {
        _status = status;
        return this;
    }

    public AppointmentBuilder WithPatient(Guid patientId)
    {
        _patientId = patientId;
        return this;
    }

    public AppointmentBuilder WithBlockId(Guid? blockId)
    {
        _blockId = blockId;
        _esBloqueCompleto = blockId.HasValue;
        return this;
    }

    public AppointmentBuilder WithTanda(Guid? tandaId)
    {
        _tandaId = tandaId;
        _esTanda = tandaId.HasValue;
        return this;
    }

    public AppointmentBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public AppointmentBuilder WithMedicoId(int? medicoId)
    {
        _medicoId = medicoId;
        return this;
    }

    public AppointmentBuilder WithMedicoUserId(Guid? medicoUserId)
    {
        _medicoUserId = medicoUserId;
        return this;
    }

    public AppointmentBuilder WithObraSocialId(int? obraSocialId)
    {
        _obraSocialId = obraSocialId;
        return this;
    }

    public AppointmentBuilder WithModalidadCobro(string modalidadCobro)
    {
        _modalidadCobro = modalidadCobro;
        return this;
    }

    public AppointmentBuilder AsLibre()
    {
        _status = AppointmentStatus.Libre;
        _patientId = null;
        return this;
    }

    public AppointmentBuilder AsOcupado(Guid patientId)
    {
        _status = AppointmentStatus.Ocupado;
        _patientId = patientId;
        return this;
    }

    public AppointmentBuilder AsCancelado()
    {
        _status = AppointmentStatus.Cancelado;
        return this;
    }

    public AppointmentBuilder AsApartado(Guid? patientId = null, Guid? apartadoPorUserId = null)
    {
        _status = AppointmentStatus.Apartado;
        _patientId = patientId;
        _apartadoPorUserId = apartadoPorUserId;
        _apartadoTs = DateTimeOffset.UtcNow;
        return this;
    }

    public AppointmentBuilder WithHoldInfo(Guid apartadoPorUserId, DateTimeOffset apartadoTs)
    {
        _apartadoPorUserId = apartadoPorUserId;
        _apartadoTs = apartadoTs;
        return this;
    }

    public AppointmentBuilder AsMonoxido(bool ordenMedica = false, bool resumenClinico = false)
    {
        _esMonoxido = true;
        _monoxidoOrdenMedica = ordenMedica;
        _monoxidoResumenClinico = resumenClinico;
        return this;
    }

    public AppointmentBuilder AsNuevoIngreso()
    {
        _esNuevoIngreso = true;
        return this;
    }

    public AppointmentBuilder WithReferente(int referenteId)
    {
        _referidoTercero = true;
        _referenteId = referenteId;
        return this;
    }

    public AppointmentBuilder WithAutorizacion(string numeroAutorizacion, int sesionesAutorizadas)
    {
        _numeroAutorizacion = numeroAutorizacion;
        _sesionesAutorizadas = sesionesAutorizadas;
        return this;
    }

    public AppointmentBuilder WithCicloObraSocial(Guid cicloObraSocialId, bool iniciarNuevoCiclo = false)
    {
        _cicloObraSocialId = cicloObraSocialId;
        _iniciarNuevoCicloObraSocial = iniciarNuevoCiclo;
        return this;
    }

    public AppointmentBuilder WithConvenioCorroborado(bool convenioCorroborado = true)
    {
        _convenioCorroborado = convenioCorroborado;
        return this;
    }

    /// <summary>
    /// Builds the Appointment entity with configured values.
    /// </summary>
    public Appointment Build()
    {
        var appointment = new Appointment(_id, _scheduleId, _fecha, _hora, _lugar, _cameraId);

        // Set private properties via reflection
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.Status), _status);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.UpdatedAt), _updatedAt);

        if (_patientId.HasValue)
        {
            EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.PatientId), _patientId.Value);
        }

        if (_blockId.HasValue)
        {
            EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.BlockId), _blockId.Value);
        }

        if (_tandaId.HasValue)
        {
            EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.TandaId), _tandaId.Value);
        }

        if (_apartadoPorUserId.HasValue)
        {
            EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.ApartadoPorUserId), _apartadoPorUserId.Value);
        }

        if (_apartadoTs.HasValue)
        {
            EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.ApartadoTs), _apartadoTs.Value);
        }

        if (_medicoUserId.HasValue)
        {
            EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.MedicoUserId), _medicoUserId.Value);
        }

        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.EsTanda), _esTanda);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.EsBloqueCompleto), _esBloqueCompleto);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.ReferidoTercero), _referidoTercero);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.ReferenteId), _referenteId);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.ModalidadCobro), _modalidadCobro);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.ObraSocialId), _obraSocialId);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.NumeroAutorizacion), _numeroAutorizacion);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.SesionesAutorizadas), _sesionesAutorizadas);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.CicloObraSocialId), _cicloObraSocialId);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.IniciarNuevoCicloObraSocial), _iniciarNuevoCicloObraSocial);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.ConvenioCorroborado), _convenioCorroborado);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.MedicoId), _medicoId);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.EsNuevoIngreso), _esNuevoIngreso);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.EsMonoxido), _esMonoxido);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.MonoxidoOrdenMedica), _monoxidoOrdenMedica);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.MonoxidoResumenClinico), _monoxidoResumenClinico);
        EntityReflectionHelper.SetProperty(appointment, nameof(Appointment.Notes), _notes);

        return appointment;
    }

    /// <summary>
    /// Implicit conversion to Appointment for cleaner test syntax.
    /// </summary>
    public static implicit operator Appointment(AppointmentBuilder builder) => builder.Build();
}
