using System.Text.Json;
using MedicalCenter.Contracts.DailyClosings;

namespace MedicalCenter.UnitTests.Contracts.DailyClosings;

public sealed class DailyClosingContractsTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public void DailyClosingPreviewResponse_DefaultCollections_AreNotNull()
    {
        var response = new DailyClosingPreviewResponse();

        Assert.NotNull(response.Alertas);
        Assert.NotNull(response.Turnos);
        Assert.Empty(response.Alertas);
        Assert.Empty(response.Turnos);
    }

    [Fact]
    public void DailyClosingPreviewResponse_Serialization_UsesSnakeCase()
    {
        var response = new DailyClosingPreviewResponse
        {
            Fecha = new DateOnly(2026, 5, 11),
            TotalTurnos = 10,
            Libres = 5,
            Ocupados = 3,
            Apartados = 1,
            Cancelados = 1,
            OcupacionPorcentaje = 30.0m,
            AptoParaCierre = true,
            GeneradoEn = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(response, _options);

        Assert.Contains("\"fecha\"", json);
        Assert.Contains("\"total_turnos\"", json);
        Assert.Contains("\"apto_para_cierre\"", json);
        Assert.Contains("\"generado_en\"", json);
    }

    [Fact]
    public void DailyClosingTurnoResponse_Serialization_UsesSnakeCase()
    {
        var response = new DailyClosingTurnoResponse
        {
            SlotId = Guid.NewGuid(),
            PacienteId = Guid.NewGuid(),
            Hora = "10:00",
            CamaraNombre = "Camara 1",
            EsNuevoIngreso = true,
            EsMonoxido = false
        };

        var json = JsonSerializer.Serialize(response, _options);

        Assert.Contains("\"slot_id\"", json);
        Assert.Contains("\"paciente_id\"", json);
        Assert.Contains("\"camara_nombre\"", json);
        Assert.Contains("\"es_nuevo_ingreso\"", json);
        Assert.Contains("\"es_monoxido\"", json);
    }
}
