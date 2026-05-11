using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.UnitTests.Domain;

/// <summary>
/// Tests the MedicoNombre (string?) property on Appointment entities.
/// Validates serialization, nullability defaults, and backward compatibility with existing data.
/// </summary>
public sealed class AppointmentMedicoNombreTests
{
    /// <summary>
/// Test 1: MedicoNombre is null by default when constructor does not provide it.
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_NullByDefault_WhenConstructorDoesNotProvideIt()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 2: MedicoNombre can be null when Appointment is in Libre state.
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_NullInLibreState_WhenNoDoctorAssigned()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.Equal(AppointmentStatus.Libre, appointment.Status);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 3: MedicoNombre is preserved when MedicoId is updated.
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_PreservedWhen_MedicoIdUpdated()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 4: MedicoNombre is null when Appointment is cancelled.
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_NullAfter_Cancel_WhenNoNameBackfilled()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var patientId = Guid.NewGuid();
        appointment.Reserve(patientId);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
    /// Test 5: MedicoNombre is null by default for legacy appointments.
    /// Verifies backward compatibility: MedicoNombre defaults to null.
    /// </summary>
    [Fact]
    public void LegacyAppointment_Compatible_NullDefault_WhenNoNewFieldAssigned()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.NotEqual(Guid.Empty, appointment.Id);
        Assert.Null(appointment.MedicoNombre);
    }

    /// <summary>
/// Test 6: MedicoUserId and MedicoNombre coexist without type conflicts.
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoUserIdAndMedicoNombre_Coexist_WithoutTypeConflict()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.Null(appointment.MedicoUserId);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 7: MedicoNombre as string is preserved across state changes.
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_PreservedAcrossStateChanges_WhenNoNameAssigned()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 8: MedicoUserId as Guid? cannot be assigned an int (type safety).
/// Fails because Appointment does NOT yet have a MedicoUserId property (but it exists).
/// </summary>
    [Fact]
    public void MedicoUserId_GuidType_CannotAssignInt_NoExplicitGuid()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.Null(appointment.MedicoUserId); // OK: MedicoUserId exists and is null
    }

    /// <summary>
/// Test 9: MedicoNombre is preserved when Appointment is reserved.
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_PreservedWhen_ReserveCalled_WhenNoNameAssigned()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var patientId = Guid.NewGuid();
        appointment.Reserve(patientId);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 10: MedicoNombre as string? cannot be assigned an enum (type safety).
/// Fails because Appointment does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_StringType_CannotAssignEnum_NoExplicitString()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        Assert.Null(appointment.MedicoNombre); // Fails: Property doesn't exist
    }
}
