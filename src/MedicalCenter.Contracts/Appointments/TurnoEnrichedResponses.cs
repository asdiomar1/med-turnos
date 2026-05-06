using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class TurnoEnrichedResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("camara_id")]
    public int? CamaraId { get; init; }

    [JsonPropertyName("lugar")]
    public int Lugar { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("paciente_id")]
    public Guid? PacienteId { get; init; }

    [JsonPropertyName("es_tanda")]
    public bool EsTanda { get; init; }

    [JsonPropertyName("tanda_id")]
    public Guid? TandaId { get; init; }

    [JsonPropertyName("es_bloque_completo")]
    public bool EsBloqueCompleto { get; init; }

    [JsonPropertyName("referido_tercero")]
    public bool? ReferidoTercero { get; init; }

    [JsonPropertyName("referente_id")]
    public int? ReferenteId { get; init; }

    [JsonPropertyName("modalidad_cobro")]
    public string? ModalidadCobro { get; init; }

    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }

    [JsonPropertyName("numero_autorizacion")]
    public string? NumeroAutorizacion { get; init; }

    [JsonPropertyName("sesiones_autorizadas")]
    public int? SesionesAutorizadas { get; init; }

    [JsonPropertyName("ciclo_obra_social_id")]
    public Guid? CicloObraSocialId { get; init; }

    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }

    [JsonPropertyName("es_nuevo_ingreso")]
    public bool? EsNuevoIngreso { get; init; }

    [JsonPropertyName("obra_social_validada_por")]
    public Guid? ObraSocialValidadaPor { get; init; }

    [JsonPropertyName("obra_social_validada_at")]
    public DateTimeOffset? ObraSocialValidadaAt { get; init; }

    [JsonPropertyName("paciente")]
    public PacienteEnrichedResponse? Paciente { get; init; }

    [JsonPropertyName("medico")]
    public MedicoEnrichedResponse? Medico { get; init; }

    [JsonPropertyName("referente")]
    public ReferenteEnrichedResponse? Referente { get; init; }

    [JsonPropertyName("camara")]
    public CamaraEnrichedResponse? Camara { get; init; }

    [JsonPropertyName("obra_social")]
    public ObraSocialEnrichedResponse? ObraSocial { get; init; }

    [JsonPropertyName("obra_social_validada_por_perfil")]
    public ObraSocialValidadaPorPerfilResponse? ObraSocialValidadaPorPerfil { get; init; }
}

public sealed class PacienteEnrichedResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }
}

public sealed class MedicoEnrichedResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("activo")]
    public bool? Activo { get; init; }
}

public sealed class ReferenteEnrichedResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; init; }

    [JsonPropertyName("activo")]
    public bool? Activo { get; init; }
}

public sealed class CamaraEnrichedResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("capacidad")]
    public int? Capacidad { get; init; }
}

public sealed class ObraSocialEnrichedResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("activa")]
    public bool? Activa { get; init; }

    [JsonPropertyName("tiene_convenio")]
    public bool? TieneConvenio { get; init; }
}

public sealed class ObraSocialValidadaPorPerfilResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }
}
