namespace MedicalCenter.Application.DTOs;

public sealed record AssignBlockAppointmentsCommand(
    DateOnly Fecha,
    TimeOnly Hora,
    int CamaraId,
    Guid PacienteId,
    bool EsTanda,
    Guid? TandaId,
    bool ReferidoTercero,
    int? ReferenteId,
    string? ModalidadCobro,
    int? ObraSocialId,
    string? NumeroAutorizacion,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId,
    bool IniciarNuevoCicloObraSocial,
    bool ConvenioCorroborado,
    int? MedicoId,
    bool EsNuevoIngreso,
    bool EsMonoxido,
    bool MonoxidoOrdenMedica,
    bool MonoxidoResumenClinico);
