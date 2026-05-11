using System.Text.Json;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.Consultations;
using MedicalCenter.Contracts.Common;

namespace MedicalCenter.Api.Mappings;

/// <summary>
/// Extension methods for mapping Application DTOs to Contract Responses.
/// Replaces private static Map() methods scattered across controllers.
/// </summary>
public static class ConsultationResponseMappings
{
    public static ConsultationScheduleHourResponse ToResponse(this ConsultationScheduleHourSummary x) =>
        new() { Id = x.Id, Hora = x.Hora, Activo = x.Activo, Orden = x.Orden, CreatedAt = x.CreatedAt };

    public static ConsultationScheduleHourDeletionPreviewResponse ToResponse(this ConsultationScheduleHourDeletionPreviewSummary x) =>
        new() { Id = x.Id, Hora = x.Hora, CanDelete = x.CanDelete, FutureSlotsCount = x.FutureSlotsCount };

    public static BlockHistoryResponse ToResponse(this BlockHistorySummary x) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Hora = x.Hora, CamaraId = x.CamaraId, SlotId = x.SlotId,
            Lugar = x.Lugar, Accion = x.Accion, PacienteId = x.PacienteId, RealizadoPor = x.RealizadoPor,
            Motivo = x.Motivo, ReferidoTercero = x.ReferidoTercero, ModalidadCobro = x.ModalidadCobro,
            ObraSocialId = x.ObraSocialId, NumeroAutorizacion = x.NumeroAutorizacion,
            ObraSocialValidadaPor = x.ObraSocialValidadaPor, ObraSocialValidadaAt = x.ObraSocialValidadaAt,
            MedicoId = x.MedicoId, MedicoUserId = x.MedicoUserId, MedicoNombre = x.MedicoNombre,
            EsNuevoIngreso = x.EsNuevoIngreso, ReferenteId = x.ReferenteId,
            TandaId = x.TandaId, SesionesAutorizadas = x.SesionesAutorizadas,
            CicloObraSocialId = x.CicloObraSocialId, CreatedAt = x.CreatedAt,
            Paciente = x.Paciente is null ? null : new BlockHistoryPatientResponse { Nombre = x.Paciente.Nombre },
            Medico = x.Medico is null ? null : new BlockHistoryMedicoResponse { Nombre = x.Medico.Nombre },
            Referente = x.Referente is null ? null : new BlockHistoryReferenteResponse { Nombre = x.Referente.Nombre, Tipo = x.Referente.Extra ?? string.Empty },
            ObraSocial = x.ObraSocial is null ? null : new BlockHistoryObraSocialResponse { Nombre = x.ObraSocial.Nombre },
            RealizadoPorPerfil = x.RealizadoPorPerfil is null ? null : new BlockHistoryPerfilResponse { Nombre = x.RealizadoPorPerfil.Nombre },
            ObraSocialValidadaPorPerfil = x.ObraSocialValidadaPorPerfil is null ? null : new BlockHistoryPerfilResponse { Nombre = x.ObraSocialValidadaPorPerfil.Nombre },
        };

    public static ConsultationSlotResponse ToResponse(this ConsultationSlotSummary x) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Hora = x.Hora, Estado = x.Estado,
            PacienteId = x.PacienteId, MedicoId = x.MedicoId, MedicoUserId = x.MedicoUserId, MedicoNombre = x.MedicoNombre,
            MotivoCancelacion = x.MotivoCancelacion, ObservacionesAdmin = x.ObservacionesAdmin,
            ConfirmadoPor = x.ConfirmadoPor, ConfirmadoAt = x.ConfirmadoAt,
            CerradoPor = x.CerradoPor, CerradoAt = x.CerradoAt,
            CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
            Paciente = x.Paciente.ToResponse(),
            Medico = x.Medico.ToResponse(),
            MedicoUser = x.MedicoUser.ToResponse(),
            ConfirmadoPorPerfil = x.ConfirmadoPorPerfil.ToResponse(),
            CerradoPorPerfil = x.CerradoPorPerfil.ToResponse(),
        };

    public static ConsultationSessionResponse ToResponse(this ConsultationSessionSummary x) =>
        new()
        {
            Id = x.Id, PacienteId = x.PacienteId, SlotId = x.SlotId, Fecha = x.Fecha,
            Hora = x.Hora, CamaraId = x.CamaraId, CreatedAt = x.CreatedAt,
            ModalidadCobro = x.ModalidadCobro, ObraSocialId = x.ObraSocialId,
            CierreId = x.CierreId, NumeroAutorizacion = x.NumeroAutorizacion,
            SesionesAutorizadas = x.SesionesAutorizadas, CicloObraSocialId = x.CicloObraSocialId,
        };

    public static OutOfHoursTurnResponse ToResponse(this OutOfHoursTurnSummary x) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Hora = x.Hora, PacienteId = x.PacienteId,
            Notas = x.Notas, CreadoPor = x.CreadoPor, OperadorCamaraId = x.OperadorCamaraId,
            CreatedAt = x.CreatedAt, EsMonoxido = x.EsMonoxido,
            MonoxidoOrdenMedica = x.MonoxidoOrdenMedica, MonoxidoResumenClinico = x.MonoxidoResumenClinico,
            MonoxidoMedicoId = x.MonoxidoMedicoId, MonoxidoMedicoUserId = x.MonoxidoMedicoUserId,
            Paciente = x.Paciente.ToResponse(),
            MonoxidoMedico = x.MonoxidoMedico.ToResponse(),
            MonoxidoMedicoUser = x.MonoxidoMedicoUser.ToResponse(),
            OperadorCamara = x.OperadorCamara.ToResponse(),
        };
}