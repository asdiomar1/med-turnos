using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Domain.Entities;

public sealed class ConsultationSlot : Entity<Guid>
{
    private ConsultationSlot() { }

    public ConsultationSlot(Guid id, DateOnly fecha, TimeOnly hora)
    {
        Id = id;
        Fecha = fecha;
        Hora = hora;
        Estado = ConsultationStatus.Libre;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public DateOnly Fecha { get; private set; }
    public TimeOnly Hora { get; private set; }
    public ConsultationStatus Estado { get; private set; }
    public Guid? PacienteId { get; private set; }
    public int? MedicoId { get; private set; }
    public string? MotivoCancelacion { get; private set; }
    public string? ObservacionesAdmin { get; private set; }
    public DateTimeOffset? ConfirmadoAt { get; private set; }
    public Guid? ConfirmadoPor { get; private set; }
    public DateTimeOffset? CerradoAt { get; private set; }
    public Guid? CerradoPor { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsReservable() => Estado is ConsultationStatus.Libre or ConsultationStatus.Cancelada;

    public bool IsConfirmable() => Estado == ConsultationStatus.Libre || Estado == ConsultationStatus.Cancelada || Estado == ConsultationStatus.Ausente;

    public bool IsClosable() => Estado == ConsultationStatus.Confirmada;

    public void Assign(Guid pacienteId, int medicoId, string? observacionesAdmin, Guid actorProfileId, DateTimeOffset now)
    {
        if (!IsReservable())
        {
            throw new InvalidOperationException("La consulta no esta disponible.");
        }

        Estado = ConsultationStatus.Confirmada;
        PacienteId = pacienteId;
        MedicoId = medicoId;
        ObservacionesAdmin = string.IsNullOrWhiteSpace(observacionesAdmin) ? null : observacionesAdmin.Trim();
        MotivoCancelacion = null;
        ConfirmadoAt = now;
        ConfirmadoPor = actorProfileId;
        CerradoAt = null;
        CerradoPor = null;
        UpdatedAt = now;
    }

    public void Cancel(string? motivo, DateTimeOffset now)
    {
        if (Estado == ConsultationStatus.Libre)
        {
            throw new InvalidOperationException("La consulta ya esta libre.");
        }

        Estado = ConsultationStatus.Cancelada;
        MotivoCancelacion = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        PacienteId = null;
        MedicoId = null;
        ObservacionesAdmin = null;
        ConfirmadoAt = null;
        ConfirmadoPor = null;
        CerradoAt = null;
        CerradoPor = null;
        UpdatedAt = now;
    }

    public void RescheduleToFreeSlot()
    {
        Estado = ConsultationStatus.Libre;
        PacienteId = null;
        MedicoId = null;
        MotivoCancelacion = null;
        ObservacionesAdmin = null;
        ConfirmadoAt = null;
        ConfirmadoPor = null;
        CerradoAt = null;
        CerradoPor = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close(string estado, Guid actorProfileId, DateTimeOffset now)
    {
        if (!IsClosable())
        {
            throw new InvalidOperationException("Solo se pueden cerrar consultas confirmadas.");
        }

        var normalized = estado.Trim().ToLowerInvariant();
        Estado = normalized switch
        {
            "completada" => ConsultationStatus.Completada,
            "ausente" => ConsultationStatus.Ausente,
            _ => throw new InvalidOperationException("Estado de cierre invalido.")
        };
        CerradoAt = now;
        CerradoPor = actorProfileId;
        UpdatedAt = now;
    }

    public void UpdateMedico(int medicoId)
    {
        MedicoId = medicoId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
