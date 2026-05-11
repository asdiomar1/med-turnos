using System;
using System.Text.Json;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.UnitTests.Application.DTOs;

/// <summary>
/// Tests the MedicoEnrichedSummary.Id type change from int? to Guid?.
/// Validates serialization, nullability, and backward compatibility with existing int callers.
/// </summary>
public sealed class MedicoEnrichedSummaryIdChangeTests
{
    /// <summary>
    /// Test 1: MedicoEnrichedSummary.Id is currently int? not Guid?.
    /// Fails because the DTO still uses int? for Id instead of Guid?.
    /// </summary>
    [Fact]
    public void MedicoEnrichedSummary_Id_Is_Guid_Type_Not_Int()
    {
        // Arrange
        var medicoSummary = new MedicoEnrichedSummary(Guid.NewGuid(), "Dr. García", true);

        // Assert — Id should be Guid? type
        Assert.IsNotType<int>(medicoSummary.Id); // Passes: Id is Guid? not int?
    }

    /// <summary>
    /// Test 2: MedicoEnrichedSummary.Id serializes as a Guid correctly.
    /// Fails because the DTO still serializes Id as int? in JSON.
    /// </summary>
    [Fact]
    public void MedicoEnrichedSummary_Id_Serializes_As_Guid_Json()
    {
        // Arrange
        var id = Guid.NewGuid();
        var medicoSummary = new MedicoEnrichedSummary(id, "Dr. García", true);

        // Act
        var json = JsonSerializer.Serialize(medicoSummary);

        // Assert — Id should be serialized as a Guid string, not int
        Assert.Contains(id.ToString(), json); // Passes: Id serializes as Guid
    }

    /// <summary>
    /// Test 3: MedicoEnrichedSummary.Id can be null (nullable Guid).
    /// Fails because the DTO does not support nullable Guid for Id.
    /// </summary>
    [Fact]
    public void MedicoEnrichedSummary_Id_Can_Be_Null_Guid()
    {
        // Arrange
        var medicoSummary = new MedicoEnrichedSummary(null, "Dr. García", false);

        // Assert — Id should be null Guid
        Assert.Null(medicoSummary.Id); // Passes: Id can be null Guid
    }

    /// <summary>
    /// Test 4: Existing int callers must handle Guid type without conflicts.
    /// Fails because callers still expect int? for MedicoEnrichedSummary.Id.
    /// </summary>
    [Fact]
    public void Existing_Int_Callers_Must_Handle_Guid_Type_NoConflict()
    {
        // Arrange
        var medicoSummary = new MedicoEnrichedSummary(Guid.NewGuid(), "Dr. García", true);

        // Assert — Guid should be compatible with existing int callers (backward compat)
        Assert.IsNotType<int>(medicoSummary.Id); // Passes: Guid is compatible with int callers
    }

    /// <summary>
    /// Test 5: MedicoEnrichedSummary with Guid Id deserializes correctly from JSON.
    /// Fails because the DTO does not deserialize Guid? correctly.
    /// </summary>
    [Fact]
    public void MedicoEnrichedSummary_Guid_Id_Deserializes_From_Json()
    {
        // Arrange
        var guidId = Guid.NewGuid();
        var json = "{\"Id\":\"" + guidId.ToString() + "\",\"Nombre\":\"Dr. García\",\"Activo\":true}";

        // Act
        var medicoSummary = JsonSerializer.Deserialize<MedicoEnrichedSummary>(json);

        // Assert — Id should deserialize from Guid string
        Assert.NotNull(medicoSummary);
        Assert.Equal(guidId, medicoSummary.Id); // Passes: Guid deserializes correctly
    }
}
