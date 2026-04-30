using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Schedule : Entity<Guid>
{
    private Schedule() { }

    public Schedule(Guid id, DateOnly fecha, TimeOnly hora, int lugar, string agendaKey)
    {
        Id = id;
        Fecha = fecha;
        Hora = hora;
        Lugar = lugar;
        AgendaKey = agendaKey;
    }

    public DateOnly Fecha { get; private set; }
    public TimeOnly Hora { get; private set; }
    public int Lugar { get; private set; }
    public string AgendaKey { get; private set; } = string.Empty;
}
