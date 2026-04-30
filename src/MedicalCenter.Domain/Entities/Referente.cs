using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Referente : Entity<int>
{
    private Referente() { }

    public Referente(string nombre, string tipo, int orden)
    {
        Nombre = nombre;
        Tipo = tipo;
        Activo = true;
        Orden = orden;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public bool Activo { get; private set; }
    public int Orden { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string nombre, string tipo)
    {
        Nombre = nombre;
        Tipo = tipo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool activo)
    {
        Activo = activo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetOrden(int orden)
    {
        Orden = orden;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
