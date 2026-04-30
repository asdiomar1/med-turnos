using MedicalCenter.Domain.Constants;

namespace MedicalCenter.Domain.Common;

public sealed record AppointmentOperativeData(
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
    bool MonoxidoResumenClinico)
{
    public static AppointmentOperativeData Empty { get; } = new(
        false,
        null,
        ModalidadCobroConstants.Default,
        null,
        null,
        null,
        null,
        false,
        false,
        null,
        false,
        false,
        false,
        false);
}
