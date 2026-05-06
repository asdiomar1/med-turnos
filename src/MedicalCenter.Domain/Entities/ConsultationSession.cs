using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Constants;

namespace MedicalCenter.Domain.Entities;

public sealed record ConsultationSessionCreateParams(
    Guid Id,
    Guid? PacienteId,
    Guid? SlotId,
    DateOnly Fecha,
    TimeOnly Hora,
    int? CamaraId,
    string ModalidadCobro,
    int? ObraSocialId,
    Guid? CierreId,
    string? NumeroAutorizacion,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId);

public sealed class ConsultationSession : Entity<Guid>
{
    private ConsultationSession() { }

    public ConsultationSession(ConsultationSessionCreateParams p)
    {
        Id = p.Id;
        PacienteId = p.PacienteId;
        SlotId = p.SlotId;
        Fecha = p.Fecha;
        Hora = p.Hora;
        CamaraId = p.CamaraId;
        ModalidadCobro = p.ModalidadCobro;
        ObraSocialId = p.ObraSocialId;
        CierreId = p.CierreId;
        NumeroAutorizacion = p.NumeroAutorizacion;
        SesionesAutorizadas = p.SesionesAutorizadas;
        CicloObraSocialId = p.CicloObraSocialId;
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
