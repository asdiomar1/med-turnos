using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class OutOfHoursTurn : Entity<Guid>
{
    private OutOfHoursTurn() { }

    public OutOfHoursTurn(
        Guid id,
        DateOnly fecha,
        TimeOnly hora,
        Guid pacienteId,
        Guid creadoPor,
        Guid operadorCamaraId,
        string? notas,
        bool esMonoxido,
        bool monoxidoOrdenMedica,
        bool monoxidoResumenClinico,
        int? monoxidoMedicoId)
    {
        Id = id;
        Fecha = fecha;
        Hora = hora;
        PacienteId = pacienteId;
        CreadoPor = creadoPor;
        OperadorCamaraId = operadorCamaraId;
        Notas = notas;
        EsMonoxido = esMonoxido;
        MonoxidoOrdenMedica = monoxidoOrdenMedica;
        MonoxidoResumenClinico = monoxidoResumenClinico;
        MonoxidoMedicoId = monoxidoMedicoId;
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
}
