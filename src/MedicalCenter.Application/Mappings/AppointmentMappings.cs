using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

/// <summary>
/// Extension methods for mapping Domain entities to Application DTOs.
/// Replaces private static Map() methods scattered across services.
/// </summary>
public static class AppointmentMappings
{
    public static AppointmentSummary ToSummary(this Appointment a) =>
        new(
            a.Id,
            a.Fecha,
            a.Hora,
            a.Lugar,
            a.Status.ToString().ToLowerInvariant(),
            a.PatientId,
            a.CameraId,
            a.BlockId,
            a.TandaId,
            a.ApartadoPorUserId,
            a.ApartadoTs,
            a.EsBloqueCompleto,
            a.EsTanda,
            a.ReferidoTercero,
            a.ReferenteId,
            a.ModalidadCobro,
            a.ObraSocialId,
            a.NumeroAutorizacion,
            a.SesionesAutorizadas,
            a.CicloObraSocialId,
            a.IniciarNuevoCicloObraSocial,
            a.ConvenioCorroborado,
            a.MedicoId,
            a.EsNuevoIngreso,
            a.EsMonoxido,
            a.MonoxidoOrdenMedica,
            a.MonoxidoResumenClinico);
}