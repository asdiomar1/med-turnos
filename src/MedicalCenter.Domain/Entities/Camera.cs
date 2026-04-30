using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Camera : Entity<int>
{
    private Camera() { }

    public Camera(int id, string nombre, int capacidad, bool activa)
    {
        Id = id;
        Nombre = nombre;
        Capacidad = capacidad;
        Activa = activa;
    }

    public string Nombre { get; private set; } = string.Empty;
    public int Capacidad { get; private set; }
    public bool Activa { get; private set; }

    public void Update(string nombre, int capacidad)
    {
        Nombre = nombre;
        Capacidad = capacidad;
    }

    public void SetActiva(bool activa)
    {
        Activa = activa;
    }
}
