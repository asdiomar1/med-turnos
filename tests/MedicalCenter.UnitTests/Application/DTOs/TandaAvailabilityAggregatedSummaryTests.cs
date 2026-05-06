using MedicalCenter.Application.DTOs;

namespace MedicalCenter.UnitTests.Application.DTOs;

public sealed class TandaAvailabilityAggregatedSummaryTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesInstance()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var hora = new TimeOnly(9, 0);
        var camaraId = 1;
        var camaraNombre = "Cámara A";
        var capacidad = 4;
        var libresCount = 2;
        var tieneDisponibilidad = true;
        var tieneBloqueCompletoPosible = false;
        var bloqueadoPorPaciente = false;

        // Act
        var result = new TandaAvailabilityAggregatedSummary(
            fecha, hora, camaraId, camaraNombre, capacidad,
            libresCount, tieneDisponibilidad, tieneBloqueCompletoPosible, bloqueadoPorPaciente);

        // Assert
        Assert.Equal(fecha, result.Fecha);
        Assert.Equal(hora, result.Hora);
        Assert.Equal(camaraId, result.CamaraId);
        Assert.Equal(camaraNombre, result.CamaraNombre);
        Assert.Equal(capacidad, result.Capacidad);
        Assert.Equal(libresCount, result.LibresCount);
        Assert.Equal(tieneDisponibilidad, result.TieneDisponibilidad);
        Assert.Equal(tieneBloqueCompletoPosible, result.TieneBloqueCompletoPosible);
        Assert.Equal(bloqueadoPorPaciente, result.BloqueadoPorPaciente);
    }

    [Fact]
    public void Constructor_WithFullCapacity_SetsTieneBloqueCompletoPosibleTrue()
    {
        // Arrange - all slots available (libres_count == capacidad)
        var fecha = new DateOnly(2024, 5, 2);
        var hora = new TimeOnly(10, 0);
        var camaraId = 2;
        var camaraNombre = "Cámara B";
        var capacidad = 4;
        var libresCount = 4; // Full capacity
        var tieneDisponibilidad = true;
        var tieneBloqueCompletoPosible = true; // All slots available
        var bloqueadoPorPaciente = false;

        // Act
        var result = new TandaAvailabilityAggregatedSummary(
            fecha, hora, camaraId, camaraNombre, capacidad,
            libresCount, tieneDisponibilidad, tieneBloqueCompletoPosible, bloqueadoPorPaciente);

        // Assert
        Assert.True(result.TieneBloqueCompletoPosible);
        Assert.True(result.TieneDisponibilidad);
    }

    [Fact]
    public void Constructor_WithZeroLibres_SetsTieneDisponibilidadFalse()
    {
        // Arrange - no slots available
        var fecha = new DateOnly(2024, 5, 2);
        var hora = new TimeOnly(11, 0);
        var camaraId = 1;
        var camaraNombre = "Cámara A";
        var capacidad = 4;
        var libresCount = 0; // No available slots
        var tieneDisponibilidad = false;
        var tieneBloqueCompletoPosible = false;
        var bloqueadoPorPaciente = false;

        // Act
        var result = new TandaAvailabilityAggregatedSummary(
            fecha, hora, camaraId, camaraNombre, capacidad,
            libresCount, tieneDisponibilidad, tieneBloqueCompletoPosible, bloqueadoPorPaciente);

        // Assert
        Assert.False(result.TieneDisponibilidad);
        Assert.False(result.TieneBloqueCompletoPosible);
    }

    [Theory]
    [InlineData("Cámara A", 1, 4)]
    [InlineData("Cámara B", 2, 6)]
    [InlineData("Cámara Principal", 99, 8)]
    public void Constructor_WithVariousCameraNames_SetsPropertiesCorrectly(string nombre, int id, int capacidad)
    {
        // Act
        var result = new TandaAvailabilityAggregatedSummary(
            new DateOnly(2024, 5, 2), new TimeOnly(9, 0), id, nombre, capacidad,
            2, true, false, false);

        // Assert
        Assert.Equal(nombre, result.CamaraNombre);
        Assert.Equal(id, result.CamaraId);
        Assert.Equal(capacidad, result.Capacidad);
    }
}
