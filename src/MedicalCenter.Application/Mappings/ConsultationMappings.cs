using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class ConsultationMappings
{
    public static ConsultationScheduleHourSummary ToSummary(this ConsultationScheduleHour x) =>
        new(x.Id, x.Hora, x.Activo, x.Orden, x.CreatedAt);

    public static ConsultationSessionSummary ToSummary(this ConsultationSession x) =>
        new(
            x.Id,
            x.PacienteId,
            x.SlotId,
            x.Fecha,
            x.Hora,
            x.CamaraId,
            x.CreatedAt,
            x.ModalidadCobro,
            x.ObraSocialId,
            x.CierreId,
            x.NumeroAutorizacion,
            x.SesionesAutorizadas,
            x.CicloObraSocialId);
}