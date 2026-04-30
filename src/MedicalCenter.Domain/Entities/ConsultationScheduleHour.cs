using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class ConsultationScheduleHour : Entity<int>
{
    private ConsultationScheduleHour() { }

    public ConsultationScheduleHour(int id, string hora, int orden, bool activo)
    {
        Id = id;
        Hora = hora;
        Orden = orden;
        Activo = activo;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Hora { get; private set; } = string.Empty;
    public int Orden { get; private set; }
    public bool Activo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string hora, int orden)
    {
        Hora = hora;
        Orden = orden;
    }

    public void SetActivo(bool activo)
    {
        Activo = activo;
    }
}
