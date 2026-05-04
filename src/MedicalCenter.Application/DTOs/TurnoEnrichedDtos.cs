namespace MedicalCenter.Application.DTOs;

/// <summary>
/// Enriched appointment DTO with nested lookup summaries for the turnos-rango-detalle feature.
/// Produced by batch-load enrichment assembly in AppointmentsService.
/// </summary>
public sealed record TurnoEnrichedSummary(
    Guid Id,
    DateOnly Fecha,
    TimeOnly Hora,
    int? CamaraId,
    int Lugar,
    string Estado,
    Guid? PacienteId,
    bool EsTanda,
    Guid? TandaId,
    bool EsBloqueCompleto,
    bool? ReferidoTercero,
    int? ReferenteId,
    string? ModalidadCobro,
    int? ObraSocialId,
    string? NumeroAutorizacion,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId,
    int? MedicoId,
    bool? EsNuevoIngreso,
    Guid? ObraSocialValidadaPor,
    DateTimeOffset? ObraSocialValidadaAt,
    PacienteEnrichedSummary? Paciente,
    MedicoEnrichedSummary? Medico,
    ReferenteEnrichedSummary? Referente,
    CamaraEnrichedSummary? Camara,
    ObraSocialEnrichedSummary? ObraSocial,
    UserBasicLookupSummary? ObraSocialValidadaPorPerfil);

public sealed record PacienteEnrichedSummary(Guid Id, string? Nombre, string? Email, int? ObraSocialId);

public sealed record MedicoEnrichedSummary(int Id, string? Nombre, bool? Activo);

public sealed record ReferenteEnrichedSummary(int Id, string? Nombre, string? Tipo, bool? Activo);

public sealed record CamaraEnrichedSummary(int Id, string? Nombre, int? Capacidad);

public sealed record ObraSocialEnrichedSummary(int Id, string? Nombre, bool? Activa, bool? TieneConvenio);

/// <summary>
/// Minimal user lookup with just Id and Nombre — avoids role loading overhead.
/// </summary>
public sealed record UserBasicLookupSummary(Guid Id, string? Nombre);

/// <summary>
/// Wrapper for paginated enriched results.
/// </summary>
public sealed record EnrichedPagedResult(
    IReadOnlyCollection<TurnoEnrichedSummary> Items,
    int Total);
