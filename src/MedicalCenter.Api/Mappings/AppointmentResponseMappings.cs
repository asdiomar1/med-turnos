using System.Text.Json;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.Appointments;
using MedicalCenter.Contracts.Common;

namespace MedicalCenter.Api.Mappings;

public static class AppointmentResponseMappings
{
    public static AppointmentResponse ToResponse(this AppointmentSummary x) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Hora = x.Hora, Lugar = x.Lugar, Estado = x.Estado,
            PacienteId = x.PacienteId, CamaraId = x.CamaraId, BlockId = x.BlockId, TandaId = x.TandaId,
            ApartadoPorUserId = x.ApartadoPorUserId, ApartadoPor = x.ApartadoPorUserId, ApartadoTs = x.ApartadoTs,
            EsBloqueCompleto = x.EsBloqueCompleto, EsTanda = x.EsTanda,
            ReferidoTercero = x.ReferidoTercero, ReferenteId = x.ReferenteId,
            ModalidadCobro = x.ModalidadCobro, ObraSocialId = x.ObraSocialId,
            NumeroAutorizacion = x.NumeroAutorizacion, SesionesAutorizadas = x.SesionesAutorizadas,
            CicloObraSocialId = x.CicloObraSocialId, IniciarNuevoCicloObraSocial = x.IniciarNuevoCicloObraSocial,
            ConvenioCorroborado = x.ConvenioCorroborado, MedicoId = x.MedicoId, MedicoUserId = x.MedicoUserId,
            MedicoNombre = x.MedicoNombre, EsNuevoIngreso = x.EsNuevoIngreso, EsMonoxido = x.EsMonoxido,
            MonoxidoOrdenMedica = x.MonoxidoOrdenMedica, MonoxidoResumenClinico = x.MonoxidoResumenClinico,
            CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
            ObraSocialValidadaPor = x.ObraSocialValidadaPor, ObraSocialValidadaAt = x.ObraSocialValidadaAt,
            Paciente = x.Paciente.ToResponse(), Medico = x.Medico.ToResponse(), Referente = x.Referente.ToResponse(),
            ObraSocial = x.ObraSocial?.ToResponse(),
            ApartadoPorPerfil = x.ApartadoPorPerfil.ToResponse(),
            ObraSocialValidadaPorPerfil = x.ObraSocialValidadaPorPerfil.ToResponse(),
        };

    public static AppointmentResponse ToResponse(this AppointmentSummary x, IReadOnlyDictionary<int, AppointmentCameraResponse> cameras) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Hora = x.Hora, Lugar = x.Lugar, Estado = x.Estado,
            PacienteId = x.PacienteId, CamaraId = x.CamaraId, BlockId = x.BlockId, TandaId = x.TandaId,
            ApartadoPorUserId = x.ApartadoPorUserId, ApartadoPor = x.ApartadoPorUserId, ApartadoTs = x.ApartadoTs,
            EsBloqueCompleto = x.EsBloqueCompleto, EsTanda = x.EsTanda,
            ReferidoTercero = x.ReferidoTercero, ReferenteId = x.ReferenteId,
            ModalidadCobro = x.ModalidadCobro, ObraSocialId = x.ObraSocialId,
            NumeroAutorizacion = x.NumeroAutorizacion, SesionesAutorizadas = x.SesionesAutorizadas,
            CicloObraSocialId = x.CicloObraSocialId, IniciarNuevoCicloObraSocial = x.IniciarNuevoCicloObraSocial,
            ConvenioCorroborado = x.ConvenioCorroborado, MedicoId = x.MedicoId, MedicoUserId = x.MedicoUserId,
            MedicoNombre = x.MedicoNombre, EsNuevoIngreso = x.EsNuevoIngreso, EsMonoxido = x.EsMonoxido,
            MonoxidoOrdenMedica = x.MonoxidoOrdenMedica, MonoxidoResumenClinico = x.MonoxidoResumenClinico,
            Camara = x.CamaraId.HasValue && cameras.TryGetValue(x.CamaraId.Value, out var found) ? found : null,
            CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
            ObraSocialValidadaPor = x.ObraSocialValidadaPor, ObraSocialValidadaAt = x.ObraSocialValidadaAt,
            Paciente = x.Paciente.ToResponse(), Medico = x.Medico.ToResponse(), Referente = x.Referente.ToResponse(),
            ObraSocial = x.ObraSocial?.ToResponse(),
            ApartadoPorPerfil = x.ApartadoPorPerfil.ToResponse(),
            ObraSocialValidadaPorPerfil = x.ObraSocialValidadaPorPerfil.ToResponse(),
        };

    public static AppointmentGroupResponse ToResponse(this AppointmentGroupSummary x) =>
        new() { Fecha = x.Fecha, Slots = x.Slots.Select(s => s.ToResponse()).ToArray() };

    public static TandaAvailabilityResponse ToResponse(this TandaAvailabilitySummary x) =>
        new() { Fecha = x.Fecha, TotalSlots = x.TotalSlots, Ocupados = x.Ocupados, Libres = x.Libres };

    public static TandaAvailabilityDetailResponse ToResponse(this TandaAvailabilityDetailSummary x) =>
        new()
        {
            Fecha = x.Fecha, Hora = x.Hora, CamaraId = x.CamaraId, Lugar = x.Lugar,
            Estado = x.Estado, TandaId = x.TandaId, PacienteId = x.PacienteId,
            EsBloqueCompleto = x.EsBloqueCompleto,
        };

    public static TandaAvailabilityAggregatedResponse ToResponse(this TandaAvailabilityAggregatedSummary x) =>
        new()
        {
            Fecha = x.Fecha,
            Hora = x.Hora,
            CamaraId = x.CamaraId,
            CamaraNombre = x.CamaraNombre,
            Capacidad = x.Capacidad,
            LibresCount = x.LibresCount,
            TieneDisponibilidad = x.TieneDisponibilidad,
            TieneBloqueCompletoPosible = x.TieneBloqueCompletoPosible,
            BloqueadoPorPaciente = x.BloqueadoPorPaciente
        };

    public static TurnoEnrichedResponse ToTurnoEnrichedResponse(this TurnoEnrichedSummary x) =>
        new()
        {
            Id = x.Id,
            Fecha = x.Fecha,
            Hora = x.Hora,
            CamaraId = x.CamaraId,
            Lugar = x.Lugar,
            Estado = x.Estado,
            PacienteId = x.PacienteId,
            EsTanda = x.EsTanda,
            TandaId = x.TandaId,
            EsBloqueCompleto = x.EsBloqueCompleto,
            ReferidoTercero = x.ReferidoTercero,
            ReferenteId = x.ReferenteId,
            ModalidadCobro = x.ModalidadCobro,
            ObraSocialId = x.ObraSocialId,
            NumeroAutorizacion = x.NumeroAutorizacion,
            SesionesAutorizadas = x.SesionesAutorizadas,
            CicloObraSocialId = x.CicloObraSocialId,
            MedicoId = x.MedicoId,
            MedicoNombre = x.Medico.Nombre,
            EsNuevoIngreso = x.EsNuevoIngreso,
            ObraSocialValidadaPor = x.ObraSocialValidadaPor,
            ObraSocialValidadaAt = x.ObraSocialValidadaAt,
            Paciente = x.Paciente is null ? null : new PacienteEnrichedResponse
            {
                Id = x.Paciente.Id,
                Nombre = x.Paciente.Nombre,
                Email = x.Paciente.Email,
                ObraSocialId = x.Paciente.ObraSocialId,
            },
            Medico = x.Medico is null ? null : new MedicoEnrichedResponse
            {
                Id = x.Medico.Id,
                Nombre = x.Medico.Nombre,
                MedicoNombre = x.Medico.Nombre,
                Activo = x.Medico.Activo,
            },
            Referente = x.Referente is null ? null : new ReferenteEnrichedResponse
            {
                Id = x.Referente.Id,
                Nombre = x.Referente.Nombre,
                Tipo = x.Referente.Tipo,
                Activo = x.Referente.Activo,
            },
            Camara = x.Camara is null ? null : new CamaraEnrichedResponse
            {
                Id = x.Camara.Id,
                Nombre = x.Camara.Nombre,
                Capacidad = x.Camara.Capacidad,
            },
            ObraSocial = x.ObraSocial is null ? null : new ObraSocialEnrichedResponse
            {
                Id = x.ObraSocial.Id,
                Nombre = x.ObraSocial.Nombre,
                Activa = x.ObraSocial.Activa,
                TieneConvenio = x.ObraSocial.TieneConvenio,
            },
            ObraSocialValidadaPorPerfil = x.ObraSocialValidadaPorPerfil is null ? null : new ObraSocialValidadaPorPerfilResponse
            {
                Id = x.ObraSocialValidadaPorPerfil.Id,
                Nombre = x.ObraSocialValidadaPorPerfil.Nombre,
            },
        };

    /// <summary>
    /// Portal-specific mapping: only exposes a subset of appointment fields
    /// (no operative data fields like ModalidadCobro, ObraSocial, etc.)
    /// </summary>
    public static AppointmentResponse ToPortalResponse(this AppointmentSummary x) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Hora = x.Hora, Lugar = x.Lugar, Estado = x.Estado,
            PacienteId = x.PacienteId, CamaraId = x.CamaraId, BlockId = x.BlockId, TandaId = x.TandaId,
            ApartadoPorUserId = x.ApartadoPorUserId, ApartadoPor = x.ApartadoPorUserId, ApartadoTs = x.ApartadoTs,
        };
}
