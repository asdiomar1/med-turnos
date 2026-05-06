using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed record OutOfHoursTurnCreateParams(
    Guid Id,
    DateOnly Fecha,
    TimeOnly Hora,
    Guid PacienteId,
    Guid CreadoPor,
    Guid OperadorCamaraId,
    string? Notas,
    bool EsMonoxido,
    bool MonoxidoOrdenMedica,
    bool MonoxidoResumenClinico,
    int? MonoxidoMedicoId,
    Guid? MonoxidoMedicoUserId = null);

public sealed class OutOfHoursTurn : Entity<Guid>
{
    private OutOfHoursTurn() { }

    public OutOfHoursTurn(OutOfHoursTurnCreateParams p)
    {
        Id = p.Id;
        Fecha = p.Fecha;
        Hora = p.Hora;
        PacienteId = p.PacienteId;
        CreadoPor = p.CreadoPor;
        OperadorCamaraId = p.OperadorCamaraId;
        Notas = p.Notas;
        EsMonoxido = p.EsMonoxido;
        MonoxidoOrdenMedica = p.MonoxidoOrdenMedica;
        MonoxidoResumenClinico = p.MonoxidoResumenClinico;
        MonoxidoMedicoId = p.MonoxidoMedicoId;
        MonoxidoMedicoUserId = p.MonoxidoMedicoUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public DateOnly Fecha { get; private set; }
    public TimeOnly Hora { get; private set; }
    public Guid PacienteId { get; private set; }
    public string? Notas { get; private set; }
    public Guid CreadoPor { get; private set; }
    public Guid OperadorCamaraId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool EsMonoxido { get; private set; }
    public bool MonoxidoOrdenMedica { get; private set; }
    public bool MonoxidoResumenClinico { get; private set; }
    public int? MonoxidoMedicoId { get; private set; }
    public Guid? MonoxidoMedicoUserId { get; private set; }
}
