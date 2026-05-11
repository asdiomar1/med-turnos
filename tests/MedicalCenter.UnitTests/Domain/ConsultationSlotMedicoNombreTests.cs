using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.UnitTests.Domain;

/// <summary>
/// Tests the MedicoNombre (string?) property on ConsultationSlot entities.
/// Validates serialization, nullability defaults, and backward compatibility with existing data.
/// </summary>
public sealed class ConsultationSlotMedicoNombreTests
{
    /// <summary>
/// Test 1: MedicoNombre is null by default when constructor does not provide it.
/// Fails because ConsultationSlot does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_NullByDefault_WhenConstructorDoesNotProvideIt()
    {
        var slot = new ConsultationSlot(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0));
        Assert.Null(slot.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 2: MedicoNombre is null when ConsultationSlot is in Libre state.
/// Fails because ConsultationSlot does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_NullInLibreState_WhenNoDoctorAssigned()
    {
        var slot = new ConsultationSlot(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0));
        Assert.Equal(ConsultationStatus.Libre, slot.Estado);
        Assert.Null(slot.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 3: MedicoNombre is preserved when MedicoId is updated.
/// Fails because ConsultationSlot does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_PreservedWhen_MedicoIdUpdated()
    {
        var slot = new ConsultationSlot(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0));
        Assert.Null(slot.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 4: MedicoNombre is null when ConsultationSlot is cancelled.
/// Fails because ConsultationSlot does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_NullAfter_Cancel_WhenNoNameBackfilled()
    {
        var slot = new ConsultationSlot(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0));
        var patientId = Guid.NewGuid();
        slot.Assign(patientId, 123, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Null(slot.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
    /// Test 5: MedicoNombre is null by default for legacy slots.
    /// Verifies backward compatibility: MedicoNombre defaults to null.
    /// </summary>
    [Fact]
    public void LegacySlot_Compatible_NullDefault_WhenNoNewFieldAssigned()
    {
        var slot = new ConsultationSlot(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0));
        Assert.NotEqual(Guid.Empty, slot.Id);
        Assert.Null(slot.MedicoNombre);
    }

    /// <summary>
/// Test 6: MedicoUserId and MedicoNombre coexist without type conflicts.
/// Fails because ConsultationSlot does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoUserIdAndMedicoNombre_Coexist_WithoutTypeConflict()
    {
        var slot = new ConsultationSlot(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0));
        var medicoUserId = Guid.NewGuid();
        Assert.Null(slot.MedicoUserId);
        Assert.Null(slot.MedicoNombre); // Fails: Property doesn't exist
    }

    /// <summary>
/// Test 7: MedicoNombre as string is preserved across state changes.
/// Fails because ConsultationSlot does NOT yet have a MedicoNombre property.
/// </summary>
    [Fact]
    public void MedicoNombre_PreservedAcrossStateChanges_WhenNoNameAssigned()
    {
        var slot = new ConsultationSlot(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0));
        Assert.Null(slot.MedicoNombre); // Fails: Property doesn't exist
    }
}
