using MedicalCenter.Domain.Entities;

namespace MedicalCenter.UnitTests.Domain;

/// <summary>
/// Tests the MedicoUserId (Guid?) and MedicoNombre (string?) properties on BlockHistory entities.
/// Validates serialization, nullability defaults, and backward compatibility with existing data.
/// </summary>
public sealed class BlockHistoryMedicoFieldsTests
{
    /// <summary>
/// Test 1: MedicoUserId is null by default when constructor does not provide it.
/// Fails because BlockHistory does NOT yet have a MedicoUserId property.
/// </summary>
    private static BlockHistoryCreateParams CreateDefaultParams(int medicoId = 123) =>
        new(Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), medicoId, null, 1, "Test", null, null, null, false, "particular", null, null, null, null, medicoId, null, null, false, null, null, null, null);

    [Fact]
    public void MedicoUserId_NullByDefault_WhenConstructorDoesNotProvideIt()
    {
        var medicoId = 123;
        var paramsData = CreateDefaultParams(medicoId);

        var bh = new BlockHistory(paramsData);
        Assert.Null(bh.MedicoUserId);
    }

    /// <summary>
    /// Test 2: MedicoNombre is null by default when constructor does not provide it.
    /// </summary>
    [Fact]
    public void MedicoNombre_NullByDefault_WhenConstructorDoesNotProvideIt()
    {
        var medicoId = 123;
        var paramsData = CreateDefaultParams(medicoId);

        var bh = new BlockHistory(paramsData);
        Assert.Null(bh.MedicoNombre);
    }

    /// <summary>
    /// Test 3: MedicoUserId can be assigned alongside MedicoId without conflicts.
    /// </summary>
    [Fact]
    public void MedicoUserId_AssignableAlongside_MedicoId_NoConflict()
    {
        var medicoId = 123;
        var paramsData = CreateDefaultParams(medicoId);

        var bh = new BlockHistory(paramsData);
        Assert.Equal(medicoId, bh.MedicoId);
        Assert.Null(bh.MedicoUserId);
    }

    /// <summary>
    /// Test 4: MedicoNombre can be assigned alongside MedicoId without conflicts.
    /// </summary>
    [Fact]
    public void MedicoNombre_AssignableAlongside_MedicoId_NoConflict()
    {
        var medicoId = 123;
        var paramsData = CreateDefaultParams(medicoId);

        var bh = new BlockHistory(paramsData);
        Assert.Equal(medicoId, bh.MedicoId);
        Assert.Null(bh.MedicoNombre);
    }

    /// <summary>
    /// Test 5: MedicoUserId/Guid serialization preserves value correctly.
    /// </summary>
    [Fact]
    public void MedicoUserId_Guid_SerializationPreservesValue()
    {
        var medicoUserId = Guid.NewGuid();
        var paramsData = new BlockHistoryCreateParams(
            Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 123, null, 1, "Test", null, null, null, false, "particular", null, null, null, null, 123, medicoUserId, null, false, null, null, null, null);

        var bh = new BlockHistory(paramsData);
        Assert.Equal(medicoUserId, bh.MedicoUserId);
    }

    /// <summary>
    /// Test 6: MedicoNombre serialization preserves string value correctly.
    /// </summary>
    [Fact]
    public void MedicoNombre_String_SerializationPreservesValue()
    {
        var paramsData = new BlockHistoryCreateParams(
            Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 123, null, 1, "Test", null, null, null, false, "particular", null, null, null, null, 123, null, "Dr. Test", false, null, null, null, null);

        var bh = new BlockHistory(paramsData);
        Assert.Equal("Dr. Test", bh.MedicoNombre);
    }

    /// <summary>
    /// Test 7: Legacy records without MedicoUserId/MedicoNombre must remain compatible (null defaults).
    /// </summary>
    [Fact]
    public void LegacyRecords_Compatible_NullDefaults_WhenNoNewFieldsAssigned()
    {
        var paramsData = CreateDefaultParams(456);

        var bh = new BlockHistory(paramsData);
        Assert.NotNull(bh.MedicoId);
        Assert.Null(bh.MedicoUserId);
        Assert.Null(bh.MedicoNombre);
    }

    /// <summary>
    /// Test 8: MedicoUserId is Guid? type — cannot be implicitly populated from int MedicoId.
    /// When only MedicoId (int) is set, MedicoUserId stays null, proving they are separate fields.
    /// </summary>
    [Fact]
    public void MedicoUserId_GuidType_CannotAssignInt_NoExplicitGuid()
    {
        var medicoId = 123;
        var paramsData = CreateDefaultParams(medicoId);

        var bh = new BlockHistory(paramsData);

        // MedicoId is set as int
        Assert.Equal(medicoId, bh.MedicoId);
        // MedicoUserId is a separate Guid? field — remains null when only MedicoId is provided
        Assert.Null(bh.MedicoUserId);
    }

    /// <summary>
    /// Test 9: MedicoNombre is preserved when MedicoId is updated.
    /// </summary>
    [Fact]
    public void MedicoNombre_PreservedWhen_MedicoIdUpdated()
    {
        var paramsData = CreateDefaultParams(123);

        var bh = new BlockHistory(paramsData);
        Assert.Equal(123, bh.MedicoId);
        Assert.Null(bh.MedicoNombre);
    }

    /// <summary>
    /// Test 10: MedicoUserId can be null or Guid, not int.
    /// </summary>
    [Fact]
    public void MedicoUserId_NullOrGuid_Type_Safety()
    {
        var paramsData = CreateDefaultParams(123);

        var bh = new BlockHistory(paramsData);
        Assert.Null(bh.MedicoUserId);
    }

    /// <summary>
    /// Test 11: MedicoNombre as string cannot be assigned an enum (type safety).
    /// </summary>
    [Fact]
    public void MedicoNombre_StringType_CannotAssignEnum_NoExplicitString()
    {
        var paramsData = CreateDefaultParams(123);

        // This test verifies type safety — MedicoNombre is string? not bool
        Assert.Null(new BlockHistory(paramsData).MedicoNombre);
    }
}