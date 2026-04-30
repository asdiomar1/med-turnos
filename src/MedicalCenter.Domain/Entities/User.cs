using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class User : Entity<Guid>
{
    private User() { }

    public User(Guid id, string identifier, string email, string passwordHash, bool isActive, bool isStaff, Guid? patientId = null, string? nombre = null)
    {
        Id = id;
        Identifier = identifier;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = isActive;
        IsStaff = isStaff;
        PatientId = patientId;
        Nombre = nombre;
    }

    public string Identifier { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsStaff { get; private set; }
    public Guid? PatientId { get; private set; }
    public string? Nombre { get; private set; }
    public List<Role> Roles { get; private set; } = [];

    public void SetRoles(IEnumerable<Role> roles)
    {
        Roles = roles.DistinctBy(x => x.Id).ToList();
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void ActivatePortalUser(string identifier, string passwordHash, string? email)
    {
        Identifier = identifier;
        PasswordHash = passwordHash;
        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email;
        }

        IsActive = true;
    }

    public void UpdateProfileName(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre es obligatorio");
        }

        Nombre = nombre.Trim();
    }

    public void LinkPatient(Guid patientId)
    {
        PatientId = patientId;
    }

    public bool HasPermission(string permission) =>
        Roles.SelectMany(x => x.Permissions).Contains(permission, StringComparer.OrdinalIgnoreCase);
}
