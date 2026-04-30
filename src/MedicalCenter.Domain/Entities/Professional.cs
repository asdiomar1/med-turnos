using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Domain.Entities;

public sealed class Professional : Entity<Guid>
{
    private Professional() { }

    public Professional(Guid id, string nombre, ProfessionalType type, bool activo)
    {
        Id = id;
        Nombre = nombre;
        Type = type;
        Activo = activo;
    }

    public string Nombre { get; private set; } = string.Empty;
    public ProfessionalType Type { get; private set; }
    public bool Activo { get; private set; }
}
