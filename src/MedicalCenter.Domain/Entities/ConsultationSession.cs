using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Constants;

namespace MedicalCenter.Domain.Entities;

public sealed class ConsultationSession : Entity<Guid>
{
    private ConsultationSession() { }

    public ConsultationSession(
        Guid id,
        Guid? pacienteId,
        Guid? slotId,
        DateOnly fecha,
        TimeOnly hora,
        int? camaraId,
        string modalidadCobro,
        int? obraSocialId,
        Guid? cierreId,
        string? numeroAutorizacion,
        int? sesionesAutorizadas,
        Guid? cicloObraSocialId)
    {
        Id = id;
        PacienteId = pacienteId;
        SlotId = slotId;
        Fecha = fecha;
        Hora = hora;
        CamaraId = camaraId;
        ModalidadCobro = modalidadCobro;
        ObraSocialId = obraSocialId;
        CierreId = cierreId;
        NumeroAutorizacion = numeroAutorizacion;
        SesionesAutorizadas = sesionesAutorizadas;
        CicloObraSocialId = cicloObraSocialId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid? PacienteId { get; private set; }
    public Guid? SlotId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public TimeOnly Hora { get; private set; }
    public int? CamaraId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string ModalidadCobro { get; private set; } = ModalidadCobroConstants.Default;
    public int? ObraSocialId { get; private set; }
    public Guid? CierreId { get; private set; }
    public string? NumeroAutorizacion { get; private set; }
    public int? SesionesAutorizadas { get; private set; }
    public Guid? CicloObraSocialId { get; private set; }
}
