using System.Text.Json;
using MedicalCenter.Contracts.Appointments;

namespace MedicalCenter.UnitTests.Contracts.Appointments;

public sealed class TurnoEnrichedResponseTests
{
    [Fact]
    public void Serialize_ToJson_UsesSnakeCasePropertyNames()
    {
        // Arrange
        var response = new TurnoEnrichedResponse
        {
            Id = Guid.NewGuid(),
            Fecha = new DateOnly(2024, 5, 2),
            Hora = new TimeOnly(9, 30),
            CamaraId = 1,
            Lugar = 2,
            Estado = "ocupado",
            PacienteId = Guid.NewGuid(),
            EsTanda = false,
            TandaId = null,
            EsBloqueCompleto = false,
            ReferidoTercero = false,
            ReferenteId = null,
            ModalidadCobro = null,
            ObraSocialId = 3,
            NumeroAutorizacion = "AUTH-123",
            SesionesAutorizadas = 10,
            CicloObraSocialId = Guid.NewGuid(),
            MedicoId = 5,
            EsNuevoIngreso = true,
            ObraSocialValidadaPor = Guid.NewGuid(),
            ObraSocialValidadaAt = new DateTimeOffset(2024, 5, 2, 14, 30, 0, TimeSpan.Zero),
            Paciente = new PacienteEnrichedResponse
            {
                Id = Guid.NewGuid(),
                Nombre = "Juan Pérez",
                Email = "juan@test.com",
                ObraSocialId = 3,
            },
            Medico = new MedicoEnrichedResponse
            {
                Id = 5,
                Nombre = "Dr. García",
                Activo = true,
            },
            Referente = new ReferenteEnrichedResponse
            {
                Id = 2,
                Nombre = "Dr. López",
                Tipo = "derivante",
                Activo = true,
            },
            Camara = new CamaraEnrichedResponse
            {
                Id = 1,
                Nombre = "Cámara 1",
                Capacidad = 4,
            },
            ObraSocial = new ObraSocialEnrichedResponse
            {
                Id = 3,
                Nombre = "OSDE",
                Activa = true,
                TieneConvenio = true,
            },
            ObraSocialValidadaPorPerfil = new ObraSocialValidadaPorPerfilResponse
            {
                Id = Guid.NewGuid(),
                Nombre = "Validador Pérez",
            },
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert — snake_case JSON property names
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"fecha\"", json);
        Assert.Contains("\"hora\"", json);
        Assert.Contains("\"camara_id\"", json);
        Assert.Contains("\"lugar\"", json);
        Assert.Contains("\"estado\"", json);
        Assert.Contains("\"paciente_id\"", json);
        Assert.Contains("\"es_tanda\"", json);
        Assert.Contains("\"tanda_id\"", json);
        Assert.Contains("\"es_bloque_completo\"", json);
        Assert.Contains("\"referido_tercero\"", json);
        Assert.Contains("\"referente_id\"", json);
        Assert.Contains("\"modalidad_cobro\"", json);
        Assert.Contains("\"obra_social_id\"", json);
        Assert.Contains("\"numero_autorizacion\"", json);
        Assert.Contains("\"sesiones_autorizadas\"", json);
        Assert.Contains("\"ciclo_obra_social_id\"", json);
        Assert.Contains("\"medico_id\"", json);
        Assert.Contains("\"es_nuevo_ingreso\"", json);
        Assert.Contains("\"obra_social_validada_por\"", json);
        Assert.Contains("\"obra_social_validada_at\"", json);
        Assert.Contains("\"paciente\"", json);
        Assert.Contains("\"medico\"", json);
        Assert.Contains("\"referente\"", json);
        Assert.Contains("\"camara\"", json);
        Assert.Contains("\"obra_social\"", json);
        Assert.Contains("\"obra_social_validada_por_perfil\"", json);

        // Verify nested objects have correct property names
        Assert.Contains("\"email\"", json);
        Assert.Contains("\"obra_social_id\"", json);
        Assert.Contains("\"tiene_convenio\"", json);
    }

    [Fact]
    public void Serialize_WhenNestedObjectsAreNull_SerializesAsNull()
    {
        // Arrange
        var response = new TurnoEnrichedResponse
        {
            Id = Guid.NewGuid(),
            Fecha = new DateOnly(2024, 5, 2),
            Hora = new TimeOnly(9, 0),
            Lugar = 1,
            Estado = "libre",
            Paciente = null,
            Medico = null,
            Referente = null,
            Camara = null,
            ObraSocial = null,
            ObraSocialValidadaPorPerfil = null,
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert — null nested objects serialize as "null", not absent
        Assert.Contains("\"paciente\":null", json);
        Assert.Contains("\"medico\":null", json);
        Assert.Contains("\"referente\":null", json);
        Assert.Contains("\"camara\":null", json);
        Assert.Contains("\"obra_social\":null", json);
        Assert.Contains("\"obra_social_validada_por_perfil\":null", json);
    }

    [Fact]
    public void Serialize_PacienteEnrichedResponse_HasCorrectProperties()
    {
        // Arrange
        var paciente = new PacienteEnrichedResponse
        {
            Id = Guid.NewGuid(),
            Nombre = "María García",
            Email = "maria@test.com",
            ObraSocialId = 2,
        };

        // Act
        var json = JsonSerializer.Serialize(paciente);

        // Assert
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"nombre\"", json);
        Assert.Contains("\"email\"", json);
        Assert.Contains("\"obra_social_id\"", json);
    }

    [Fact]
    public void Serialize_MedicoEnrichedResponse_HasCorrectProperties()
    {
        // Arrange
        var medico = new MedicoEnrichedResponse
        {
            Id = 1,
            Nombre = "Dr. García",
            Activo = true,
        };

        // Act
        var json = JsonSerializer.Serialize(medico);

        // Assert
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"nombre\"", json);
        Assert.Contains("\"activo\"", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void Serialize_ObraSocialValidadaPorPerfilResponse_HasCorrectProperties()
    {
        // Arrange
        var perfil = new ObraSocialValidadaPorPerfilResponse
        {
            Id = Guid.NewGuid(),
            Nombre = "Validador",
        };

        // Act
        var json = JsonSerializer.Serialize(perfil);

        // Assert
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"nombre\"", json);
    }
}
