using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Constants;

namespace MedicalCenter.Domain.Entities;

public sealed record BlockHistoryCreateParams(
    Guid Id,
    DateOnly Fecha,
    TimeOnly Hora,
    int? CamaraId,
    Guid? SlotId,
    int? Lugar,
    string Accion,
    Guid? PacienteId,
    Guid? RealizadoPor,
    string? Motivo,
    bool ReferidoTercero,
    string ModalidadCobro,
    int? ObraSocialId,
    string? NumeroAutorizacion,
    Guid? ObraSocialValidadaPor,
    DateTimeOffset? ObraSocialValidadaAt,
    int? MedicoId,
    Guid? MedicoUserId,
    string? MedicoNombre,
    bool EsNuevoIngreso,
    int? ReferenteId,
    Guid? TandaId,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId);

public sealed class BlockHistory : Entity<Guid>
{
    private BlockHistory() { }

    public BlockHistory(BlockHistoryCreateParams p)
    {
        Id = p.Id;
        Fecha = p.Fecha;
        Hora = p.Hora;
        CamaraId = p.CamaraId;
        SlotId = p.SlotId;
        Lugar = p.Lugar;
        Accion = p.Accion;
        PacienteId = p.PacienteId;
        RealizadoPor = p.RealizadoPor;
        Motivo = p.Motivo;
        ReferidoTercero = p.ReferidoTercero;
        ModalidadCobro = p.ModalidadCobro;
        ObraSocialId = p.ObraSocialId;
        NumeroAutorizacion = p.NumeroAutorizacion;
        ObraSocialValidadaPor = p.ObraSocialValidadaPor;
        ObraSocialValidadaAt = p.ObraSocialValidadaAt;
        MedicoId = p.MedicoId;
        MedicoUserId = p.MedicoUserId;
        MedicoNombre = p.MedicoNombre;
        EsNuevoIngreso = p.EsNuevoIngreso;
        ReferenteId = p.ReferenteId;
        TandaId = p.TandaId;
        SesionesAutorizadas = p.SesionesAutorizadas;
        CicloObraSocialId = p.CicloObraSocialId;
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
    public Guid? MedicoUserId { get; private set; }
    public string? MedicoNombre { get; private set; }
    public bool EsNuevoIngreso { get; private set; }
    public int? ReferenteId { get; private set; }
    public Guid? TandaId { get; private set; }
    public int? SesionesAutorizadas { get; private set; }
    public Guid? CicloObraSocialId { get; private set; }
}
