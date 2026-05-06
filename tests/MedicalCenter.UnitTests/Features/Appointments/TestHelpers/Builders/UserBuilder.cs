using MedicalCenter.Domain.Entities;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;

namespace MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating User entities in tests.
/// </summary>
public sealed class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _username = "testuser";
    private string _email = "test@medicalcenter.local";
    private bool _isActive = true;
    private bool _isStaff = true;
    private Guid? _patientId;
    private readonly List<string> _permissions = new();
    private readonly List<Role> _roles = new();

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPermission(string permission)
    {
        _permissions.Add(permission);
        return this;
    }

    public UserBuilder WithPermissions(params string[] permissions)
    {
        _permissions.AddRange(permissions);
        return this;
    }

    public UserBuilder AsStaff()
    {
        _isStaff = true;
        return this;
    }

    public UserBuilder AsPatient()
    {
        _isStaff = false;
        return this;
    }

    public UserBuilder Active()
    {
        _isActive = true;
        return this;
    }

    public UserBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    public UserBuilder AsAdmin()
    {
        _isStaff = true;
        _permissions.Add("admin.full");
        return this;
    }

    public UserBuilder AsMedico()
    {
        _isStaff = true;
        _permissions.Add("medico");
        return this;
    }

    public UserBuilder WithPatientId(Guid? patientId)
    {
        _patientId = patientId;
        return this;
    }

    /// <summary>
    /// Builds the User entity with configured values.
    /// </summary>
    public User Build()
    {
        var user = new User(new UserCreateParams(
            _id,
            _username,
            _email,
            "testhash",
            _isActive,
            _isStaff,
            _patientId));

        if (_permissions.Count > 0 || _roles.Count > 0)
        {
            var roles = _roles.ToList();

            if (_permissions.Count > 0 && roles.Count == 0)
            {
                // Create a role with the specified permissions
                var role = new Role(new RoleCreateParams(
                    Guid.NewGuid(),
                    "testrole",
                    "Test Role",
                    _permissions.ToList()));
                roles.Add(role);
            }

            if (roles.Count > 0)
            {
                user.SetRoles(roles);
            }
        }

        return user;
    }

    /// <summary>
    /// Implicit conversion to User for cleaner test syntax.
    /// </summary>
    public static implicit operator User(UserBuilder builder) => builder.Build();

    /// <summary>
    /// Creates a default staff user with the specified permission.
    /// </summary>
    public static UserBuilder StaffWithPermission(string permission)
    {
        return new UserBuilder()
            .AsStaff()
            .WithPermission(permission);
    }

    /// <summary>
    /// Creates a default staff actor for appointment operations.
    /// </summary>
    public static UserBuilder StaffActor(string permission) => StaffWithPermission(permission);
}
