using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Medico : Entity<int>
{
    private Medico() { }

    public Medico(string nombre, int orden, Guid? perfilId = null)
    {
        Nombre = nombre;
        Activo = true;
        Orden = orden;
        PerfilId = perfilId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Nombre { get; private set; } = string.Empty;
    public bool Activo { get; private set; }
    public int Orden { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? PerfilId { get; private set; }
    public Guid? IdGuid { get; private set; }

    public void UpdateNombre(string nombre)
    {
        Nombre = nombre;
    }

    public void SetActive(bool activo) => Activo = activo;
    public void SetOrden(int orden) => Orden = orden;
    public void SetPerfil(Guid? perfilId) => PerfilId = perfilId;
    public void SetIdGuid(Guid? idGuid) => IdGuid = idGuid;
}
