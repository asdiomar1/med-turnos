using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.Appointments;

namespace MedicalCenter.UnitTests.Api.Mappings;

/// <summary>
/// Approval tests for AppointmentResponseMappings overloads.
/// These tests capture current behavior before S4136 reorder (T16).
/// </summary>
public sealed class AppointmentResponseMappingsTests
{
    [Fact]
    public void ToResponse_AppointmentGroupSummary_ReturnsGroupResponse()
    {
        var summary = new AppointmentGroupSummary(
            new DateOnly(2026, 5, 7),
            Array.Empty<AppointmentSummary>());

        var result = summary.ToResponse();

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 7), result.Fecha);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public void ToResponse_TandaAvailabilitySummary_ReturnsAvailabilityResponse()
    {
        var summary = new TandaAvailabilitySummary(
            new DateOnly(2026, 5, 7), 10, 3, 7);

        var result = summary.ToResponse();

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 7), result.Fecha);
        Assert.Equal(10, result.TotalSlots);
        Assert.Equal(3, result.Ocupados);
        Assert.Equal(7, result.Libres);
    }

    [Fact]
    public void ToResponse_TandaAvailabilityDetailSummary_ReturnsDetailResponse()
    {
        var summary = new TandaAvailabilityDetailSummary(
            new DateOnly(2026, 5, 7),
            new TimeOnly(10, 0),
            1, 5, "LIBRE", null, null, false);

        var result = summary.ToResponse();

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 7), result.Fecha);
        Assert.Equal(new TimeOnly(10, 0), result.Hora);
        Assert.Equal(1, result.CamaraId);
        Assert.Equal(5, result.Lugar);
        Assert.Equal("LIBRE", result.Estado);
    }

    [Fact]
    public void ToResponse_TandaAvailabilityAggregatedSummary_ReturnsAggregatedResponse()
    {
        var summary = new TandaAvailabilityAggregatedSummary(
            new DateOnly(2026, 5, 7),
            new TimeOnly(10, 0),
            1, "Camara 1", 10, 5, true, false, false);

        var result = summary.ToResponse();

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 7), result.Fecha);
        Assert.Equal(new TimeOnly(10, 0), result.Hora);
        Assert.Equal(1, result.CamaraId);
        Assert.Equal("Camara 1", result.CamaraNombre);
        Assert.Equal(10, result.Capacidad);
        Assert.Equal(5, result.LibresCount);
        Assert.True(result.TieneDisponibilidad);
        Assert.False(result.TieneBloqueCompletoPosible);
        Assert.False(result.BloqueadoPorPaciente);
    }
}
