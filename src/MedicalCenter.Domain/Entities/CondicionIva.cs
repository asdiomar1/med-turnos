using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class CondicionIva : Entity<int>
{
    private CondicionIva() { }

    public CondicionIva(int id, string nombre, bool activo = true, int orden = 0)
    {
        Id = id;
        Nombre = nombre;
        Activo = activo;
        Orden = orden;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Nombre { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;
    public int Orden { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
