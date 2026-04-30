using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Constants;

namespace MedicalCenter.Domain.Entities;

public sealed class BlockHistory : Entity<Guid>
{
    private BlockHistory() { }

    public BlockHistory(
        Guid id,
        DateOnly fecha,
        TimeOnly hora,
        int? camaraId,
        Guid? slotId,
        int? lugar,
        string accion,
        Guid? pacienteId,
        Guid? realizadoPor,
        string? motivo,
        bool referidoTercero,
        string modalidadCobro,
        int? obraSocialId,
        string? numeroAutorizacion,
        Guid? obraSocialValidadaPor,
        DateTimeOffset? obraSocialValidadaAt,
        int? medicoId,
        bool esNuevoIngreso,
        int? referenteId,
        Guid? tandaId,
        int? sesionesAutorizadas,
        Guid? cicloObraSocialId)
    {
        Id = id;
        Fecha = fecha;
        Hora = hora;
        CamaraId = camaraId;
        SlotId = slotId;
        Lugar = lugar;
        Accion = accion;
        PacienteId = pacienteId;
        RealizadoPor = realizadoPor;
        Motivo = motivo;
        ReferidoTercero = referidoTercero;
        ModalidadCobro = modalidadCobro;
        ObraSocialId = obraSocialId;
        NumeroAutorizacion = numeroAutorizacion;
        ObraSocialValidadaPor = obraSocialValidadaPor;
        ObraSocialValidadaAt = obraSocialValidadaAt;
        MedicoId = medicoId;
        EsNuevoIngreso = esNuevoIngreso;
        ReferenteId = referenteId;
        TandaId = tandaId;
        SesionesAutorizadas = sesionesAutorizadas;
        CicloObraSocialId = cicloObraSocialId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public DateOnly Fecha { get; private set; }
    public TimeOnly Hora { get; private set; }
    public int? CamaraId { get; private set; }
    public Guid? SlotId { get; private set; }
    public int? Lugar { get; private set; }
    public string Accion { get; private set; } = string.Empty;
    public Guid? PacienteId { get; private set; }
    public Guid? RealizadoPor { get; private set; }
    public string? Motivo { get; private set; }
    public bool ReferidoTercero { get; private set; }
    public string ModalidadCobro { get; private set; } = ModalidadCobroConstants.Default;
    public int? ObraSocialId { get; private set; }
    public string? NumeroAutorizacion { get; private set; }
    public Guid? ObraSocialValidadaPor { get; private set; }
    public DateTimeOffset? ObraSocialValidadaAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int? MedicoId { get; private set; }
    public bool EsNuevoIngreso { get; private set; }
    public int? ReferenteId { get; private set; }
    public Guid? TandaId { get; private set; }
    public int? SesionesAutorizadas { get; private set; }
    public Guid? CicloObraSocialId { get; private set; }
}
