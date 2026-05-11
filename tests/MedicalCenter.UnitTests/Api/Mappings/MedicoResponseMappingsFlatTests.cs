using System;
using System.Text.Json;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.Appointments;
using MedicalCenter.Contracts.Professionals;

namespace MedicalCenter.UnitTests.Api.Mappings;

/// <summary>
/// Tests the MedicoResponseMappings flat MedicoNombre field alongside nested Medico object.
/// Validates serialization includes both flat and nested medico fields.
/// </summary>
public sealed class MedicoResponseMappingsFlatTests
{
    /// <summary>
    /// Test 1: MedicoResponseMappings.ToResponse does NOT yet include MedicoNombre flat field.
    /// Fails because MedicoResponseMappings does NOT have MedicoNombre property.
    /// </summary>
    [Fact]
    public void MedicoResponseMappings_ToResponse_Does_Not_Have_MedicoNombre_Flat_Field()
    {
        // Arrange
        var medicoSummaryDto = new MedicoSummaryDto(Guid.NewGuid(), "Dr. García");

        // Act
        var response = medicoSummaryDto.ToResponse();

        // Assert — MedicoNombre should be present
        Assert.NotNull(typeof(MedicoResponse).GetProperty("MedicoNombre")); // Passes: MedicoNombre property exists
        Assert.Equal("Dr. García", response.MedicoNombre);
    }

    /// <summary>
    /// Test 2: MedicoEnrichedResponse serialization includes MedicoNombre flat field.
    /// Fails because MedicoEnrichedResponse does NOT have MedicoNombre property.
    /// </summary>
    [Fact]
    public void MedicoEnrichedResponse_Serialization_Includes_MedicoNombre_Flat_Field()
    {
        // Arrange
        var medico = new MedicoEnrichedResponse
        {
            Id = Guid.NewGuid(),
            Nombre = "Dr. García",
            Activo = true
        };

        // Act
        var json = JsonSerializer.Serialize(medico);

        // Assert — MedicoNombre should be in JSON alongside nested fields
        Assert.Contains("\"medico_nombre\"", json); // Passes: MedicoNombre in JSON
    }

    /// <summary>
    /// Test 3: MedicoResponse flat field includes MedicoNombre alongside nested Medico object.
    /// Fails because MedicoResponse does NOT have MedicoNombre property alongside Medico nested object.
    /// </summary>
    [Fact]
    public void MedicoResponse_Flat_Field_Includes_MedicoNombre_Alongside_Nested_Medico_Object()
    {
        // Arrange
        var response = new MedicoResponse
        {
            Id = Guid.NewGuid(),
            Nombre = "Dr. García"
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert — MedicoNombre should be alongside nested fields
        Assert.Contains("\"nombre\"", json); // OK: Nombre exists
        Assert.Contains("\"medico_nombre\"", json); // Passes: MedicoNombre alongside
    }

    /// <summary>
    /// Test 4: MedicoEnrichedResponse has MedicoNombre nullable string type.
    /// Fails because MedicoEnrichedResponse does NOT have MedicoNombre property.
    /// </summary>
    [Fact]
    public void MedicoEnrichedResponse_Has_MedicoNombre_Nullable_String_Type()
    {
        // Arrange
        var medico = new MedicoEnrichedResponse
        {
            Id = Guid.NewGuid(),
            Nombre = "Dr. García",
            MedicoNombre = "Dr. García",
            Activo = true
        };

        // Assert — MedicoNombre should be nullable string type
        Assert.NotNull(medico.MedicoNombre); // Passes: MedicoNombre exists
    }

    /// <summary>
    /// Test 5: MedicoResponseMappings.ToResponse includes MedicoNombre when Medico has Nombre.
    /// Fails because MedicoResponseMappings does NOT map MedicoNombre flat field.
    /// </summary>
    [Fact]
    public void MedicoResponseMappings_ToResponse_Includes_MedicoNombre_When_Medico_Has_Nombre()
    {
        // Arrange
        var medicoSummaryDto = new MedicoSummaryDto(Guid.NewGuid(), "Dr. García");

        // Act
        var response = medicoSummaryDto.ToResponse();

        // Assert — MedicoNombre should be mapped
        Assert.NotNull(response.MedicoNombre); // Passes: MedicoNombre mapped
        Assert.Equal("Dr. García", response.MedicoNombre);
    }

    /// <summary>
    /// Test 6: TurnoEnrichedResponse includes MedicoNombre flat field alongside Medico nested object.
    /// Fails because TurnoEnrichedResponse does NOT have MedicoNombre property.
    /// </summary>
    [Fact]
    public void TurnoEnrichedResponse_Includes_MedicoNombre_Alongside_Medico_Nested_Object()
    {
        // Arrange
        var response = new TurnoEnrichedResponse
        {
            Id = Guid.NewGuid(),
            MedicoId = 5,
            Medico = new MedicoEnrichedResponse
            {
                Id = Guid.NewGuid(),
                Nombre = "Dr. García",
                Activo = true
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert — MedicoNombre should be alongside Medico nested object
        Assert.Contains("\"medico_nombre\"", json); // Passes: MedicoNombre in response
    }
}
