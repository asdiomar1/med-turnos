using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class CampoConfig : Entity<Guid>
{
    private CampoConfig() { }

    public CampoConfig(string nombre, string tipo, int orden)
    {
        Id = Guid.NewGuid();
        Nombre = nombre;
        Tipo = tipo;
        Orden = orden;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public int Orden { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string nombre, string tipo)
    {
        Nombre = nombre;
        Tipo = tipo;
    }

    public void SetOrden(int orden) => Orden = orden;
}
