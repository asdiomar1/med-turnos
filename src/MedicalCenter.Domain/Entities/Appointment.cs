using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Constants;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Domain.Entities;

public sealed class Appointment : Entity<Guid>
{
    private Appointment() { }

    public Appointment(Guid id, Guid scheduleId, DateOnly fecha, TimeOnly hora, int lugar, int? cameraId = null)
    {
        Id = id;
        ScheduleId = scheduleId;
        Fecha = fecha;
        Hora = hora;
        Lugar = lugar;
        CameraId = cameraId;
        Status = AppointmentStatus.Libre;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid ScheduleId { get; private set; }
    public Guid? PatientId { get; private set; }
    public Guid? ProfessionalId { get; private set; }
    public Guid? BlockId { get; private set; }
    public Guid? TandaId { get; private set; }
    public int? CameraId { get; private set; }
    public bool EsBloqueCompleto { get; private set; }
    public bool EsTanda { get; private set; }
    public bool ReferidoTercero { get; private set; }
    public int? ReferenteId { get; private set; }
    public string ModalidadCobro { get; private set; } = ModalidadCobroConstants.Default;
    public int? ObraSocialId { get; private set; }
    public string? NumeroAutorizacion { get; private set; }
    public int? SesionesAutorizadas { get; private set; }
    public Guid? CicloObraSocialId { get; private set; }
    public bool IniciarNuevoCicloObraSocial { get; private set; }
    public bool ConvenioCorroborado { get; private set; }
    public int? MedicoId { get; private set; }
    public Guid? MedicoUserId { get; private set; }
    public string? MedicoNombre { get; private set; }
    public bool EsNuevoIngreso { get; private set; }
    public bool EsMonoxido { get; private set; }
    public bool MonoxidoOrdenMedica { get; private set; }
    public bool MonoxidoResumenClinico { get; private set; }
    public DateOnly Fecha { get; private set; }
    public TimeOnly Hora { get; private set; }
    public int Lugar { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ApartadoPorUserId { get; private set; }
    public DateTimeOffset? ApartadoTs { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsReservable() => Status is AppointmentStatus.Libre or AppointmentStatus.Cancelado or AppointmentStatus.Reprogramado;
    public bool IsOccupied() => Status is AppointmentStatus.Ocupado;

    public void Reserve(Guid patientId, string? notes = null, bool esTanda = false, Guid? tandaId = null, AppointmentOperativeData? operative = null)
    {
        if (!IsReservable())
        {
            throw new InvalidOperationException("El turno ya no esta disponible.");
        }

        PatientId = patientId;
        Status = AppointmentStatus.Ocupado;
        Notes = notes;
        EsBloqueCompleto = false;
        EsTanda = esTanda || tandaId.HasValue;
        TandaId = tandaId;
        ApartadoPorUserId = null;
        ApartadoTs = null;
        ApplyOperativeData(operative);
        Touch();
    }

    public void Hold(Guid? patientId, Guid userId, DateTimeOffset apartadoTs, string? notes = null, bool esMonoxido = false, Guid? tandaId = null, AppointmentOperativeData? operative = null)
    {
        if (!IsReservable())
        {
            throw new InvalidOperationException("El turno ya no esta disponible.");
        }

        PatientId = patientId;
        Status = AppointmentStatus.Apartado;
        ApartadoPorUserId = userId;
        ApartadoTs = apartadoTs;
        Notes = notes;
        EsMonoxido = esMonoxido;
        TandaId = tandaId;
        EsTanda = tandaId.HasValue;
        ApplyOperativeData(operative);
        Touch();
    }

    public void ConfirmHold(Guid? patientId = null, string? notes = null, AppointmentOperativeData? operative = null)
    {
        if (Status != AppointmentStatus.Apartado)
        {
            throw new InvalidOperationException("Solo se pueden confirmar slots en estado apartado.");
        }

        var finalPatientId = patientId ?? PatientId;
        if (!finalPatientId.HasValue)
        {
            throw new InvalidOperationException("Paciente requerido para confirmar el apartado.");
        }

        PatientId = finalPatientId.Value;
        Status = AppointmentStatus.Ocupado;
        Notes = notes ?? Notes;
        ApartadoPorUserId = null;
        ApartadoTs = null;
        ApplyOperativeData(operative);
        Touch();
    }

    public void ReleaseHold(string? notes = null)
    {
        if (Status != AppointmentStatus.Apartado)
        {
            throw new InvalidOperationException("Solo se pueden liberar slots en estado apartado.");
        }

        PatientId = null;
        Status = AppointmentStatus.Libre;
        Notes = notes;
        ApartadoPorUserId = null;
        ApartadoTs = null;
        EsBloqueCompleto = false;
        EsTanda = false;
        TandaId = null;
        Touch();
    }

    public void Reschedule(string? notes = null)
    {
        if (Status != AppointmentStatus.Ocupado && Status != AppointmentStatus.Apartado)
        {
            throw new InvalidOperationException("Solo se pueden reprogramar turnos ocupados o apartados.");
        }

        Status = AppointmentStatus.Reprogramado;
        PatientId = null;
        ApartadoPorUserId = null;
        ApartadoTs = null;
        Notes = notes;
        Touch();
    }

    public void Cancel(string? notes = null)
    {
        if (!IsOccupied())
        {
            throw new InvalidOperationException("Solo se pueden cancelar turnos ocupados.");
        }

        PatientId = null;
        ProfessionalId = null;
        BlockId = null;
        TandaId = null;
        EsBloqueCompleto = false;
        EsTanda = false;
        Status = AppointmentStatus.Cancelado;
        Notes = notes;
        ApartadoPorUserId = null;
        ApartadoTs = null;
        Touch();
    }

    public void Release(string? notes = null)
    {
        if (!IsOccupied() && Status != AppointmentStatus.Apartado)
        {
            throw new InvalidOperationException("Solo se pueden liberar turnos ocupados o apartados.");
        }

        PatientId = null;
        ProfessionalId = null;
        BlockId = null;
        TandaId = null;
        EsBloqueCompleto = false;
        EsTanda = false;
        Status = AppointmentStatus.Libre;
        Notes = notes;
        ApartadoPorUserId = null;
        ApartadoTs = null;
        ApplyOperativeData(AppointmentOperativeData.Empty);
        Touch();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        Touch();
    }

    public void AssignBlock(Guid blockId, bool esTanda = false, Guid? tandaId = null, AppointmentOperativeData? operative = null)
    {
        BlockId = blockId;
        EsBloqueCompleto = true;
        EsTanda = esTanda || tandaId.HasValue;
        TandaId = tandaId;
        ApplyOperativeData(operative);
        Touch();
    }

    public void AssignTanda(Guid? tandaId)
    {
        TandaId = tandaId;
        EsTanda = tandaId.HasValue;
        Touch();
    }

    public void UpdateOperativeData(AppointmentOperativeData operative)
    {
        ApplyOperativeData(operative);
        Touch();
    }

    private void ApplyOperativeData(AppointmentOperativeData? operative)
    {
        operative ??= AppointmentOperativeData.Empty;

        ReferidoTercero = operative.ReferidoTercero;
        ReferenteId = operative.ReferenteId;
        ModalidadCobro = string.IsNullOrWhiteSpace(operative.ModalidadCobro) ? ModalidadCobroConstants.Default : operative.ModalidadCobro.Trim();
        ObraSocialId = operative.ObraSocialId;
        NumeroAutorizacion = operative.NumeroAutorizacion;
        SesionesAutorizadas = operative.SesionesAutorizadas;
        CicloObraSocialId = operative.CicloObraSocialId;
        IniciarNuevoCicloObraSocial = operative.IniciarNuevoCicloObraSocial;
        ConvenioCorroborado = operative.ConvenioCorroborado;
        MedicoId = operative.MedicoId;
        MedicoUserId = operative.MedicoUserId;
        EsNuevoIngreso = operative.EsNuevoIngreso;
        EsMonoxido = operative.EsMonoxido;
        MonoxidoOrdenMedica = operative.MonoxidoOrdenMedica;
        MonoxidoResumenClinico = operative.MonoxidoResumenClinico;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
