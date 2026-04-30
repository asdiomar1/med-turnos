using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class ObraSocial : Entity<int>
{
    private ObraSocial() { }

    public ObraSocial(int id = 0, string nombre = "", bool activa = true, bool tieneConvenio = false, int orden = 0, string? abreviatura = null)
    {
        Id = id;
        Nombre = nombre;
        Activa = activa;
        TieneConvenio = tieneConvenio;
        Orden = orden;
        Abreviatura = abreviatura;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Nombre { get; private set; } = string.Empty;
    public bool Activa { get; private set; } = true;
    public bool TieneConvenio { get; private set; }
    public int Orden { get; private set; }
    public string? Abreviatura { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string nombre, bool tieneConvenio, string? abreviatura)
    {
        Nombre = nombre;
        TieneConvenio = tieneConvenio;
        Abreviatura = string.IsNullOrWhiteSpace(abreviatura) ? null : abreviatura.Trim().ToUpperInvariant();
    }

    public void SetActive(bool activa) => Activa = activa;
    public void SetTieneConvenio(bool tieneConvenio) => TieneConvenio = tieneConvenio;
}
