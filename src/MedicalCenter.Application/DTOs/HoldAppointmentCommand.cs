namespace MedicalCenter.Application.DTOs;

public sealed record HoldAppointmentCommand(
    Guid? PacienteId,
    bool EsMonoxido,
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
    bool MonoxidoOrdenMedica,
    bool MonoxidoResumenClinico);
