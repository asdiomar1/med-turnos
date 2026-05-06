using System.Text.Json;
using MedicalCenter.Contracts.Appointments;

namespace MedicalCenter.UnitTests.Contracts.Appointments;

public sealed class TandaAvailabilityAggregatedResponseTests
{
    [Fact]
    public void Serialize_ToJson_MatchesExpectedFormat()
    {
        // Arrange
        var response = new TandaAvailabilityAggregatedResponse
        {
            Fecha = new DateOnly(2024, 5, 2),
            Hora = new TimeOnly(9, 0),
            CamaraId = 1,
            CamaraNombre = "Cámara A",
            Capacidad = 4,
            LibresCount = 2,
            TieneDisponibilidad = true,
            TieneBloqueCompletoPosible = false,
            BloqueadoPorPaciente = false
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert - Verify JSON contains expected property names from JsonPropertyName attributes
        Assert.Contains("fecha", json);
        Assert.Contains("hora", json);
        Assert.Contains("camara_id", json);
        Assert.Contains("camara_nombre", json);
        Assert.Contains("capacidad", json);
        Assert.Contains("libres_count", json);
        Assert.Contains("tiene_disponibilidad", json);
        Assert.Contains("tiene_bloque_completo_posible", json);
        Assert.Contains("bloqueado_paciente", json);
    }

    [Fact]
    public void Properties_WithFullCapacity_SetsCorrectValues()
    {
        // Arrange & Act
        var response = new TandaAvailabilityAggregatedResponse
        {
            Fecha = new DateOnly(2024, 5, 2),
            Hora = new TimeOnly(10, 0),
            CamaraId = 2,
            CamaraNombre = "Cámara B",
            Capacidad = 6,
            LibresCount = 6,
            TieneDisponibilidad = true,
            TieneBloqueCompletoPosible = true,
            BloqueadoPorPaciente = false
        };

        // Assert
        Assert.Equal(6, response.LibresCount);
        Assert.Equal(6, response.Capacidad);
        Assert.True(response.TieneBloqueCompletoPosible);
    }

    [Fact]
    public void Properties_WhenBlockedByPatient_SetsFlagTrue()
    {
        // Arrange & Act
        var response = new TandaAvailabilityAggregatedResponse
        {
            Fecha = new DateOnly(2024, 5, 2),
            Hora = new TimeOnly(11, 0),
            CamaraId = 1,
            CamaraNombre = "Cámara A",
            Capacidad = 4,
            LibresCount = 2,
            TieneDisponibilidad = true,
            TieneBloqueCompletoPosible = false,
            BloqueadoPorPaciente = true
        };

        // Assert
        Assert.True(response.BloqueadoPorPaciente);
    }
}
