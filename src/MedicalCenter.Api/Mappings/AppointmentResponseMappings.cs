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
            ApartadoPorUserId = x.ApartadoPorUserId, ApartadoTs = x.ApartadoTs,
            EsBloqueCompleto = x.EsBloqueCompleto, EsTanda = x.EsTanda,
            ReferidoTercero = x.ReferidoTercero, ReferenteId = x.ReferenteId,
            ModalidadCobro = x.ModalidadCobro, ObraSocialId = x.ObraSocialId,
            NumeroAutorizacion = x.NumeroAutorizacion, SesionesAutorizadas = x.SesionesAutorizadas,
            CicloObraSocialId = x.CicloObraSocialId, IniciarNuevoCicloObraSocial = x.IniciarNuevoCicloObraSocial,
            ConvenioCorroborado = x.ConvenioCorroborado, MedicoId = x.MedicoId,
            EsNuevoIngreso = x.EsNuevoIngreso, EsMonoxido = x.EsMonoxido,
            MonoxidoOrdenMedica = x.MonoxidoOrdenMedica, MonoxidoResumenClinico = x.MonoxidoResumenClinico,
        };

    public static AppointmentResponse ToResponse(this AppointmentSummary x, IReadOnlyDictionary<int, AppointmentCameraResponse> cameras) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Hora = x.Hora, Lugar = x.Lugar, Estado = x.Estado,
            PacienteId = x.PacienteId, CamaraId = x.CamaraId, BlockId = x.BlockId, TandaId = x.TandaId,
            ApartadoPorUserId = x.ApartadoPorUserId, ApartadoTs = x.ApartadoTs,
            EsBloqueCompleto = x.EsBloqueCompleto, EsTanda = x.EsTanda,
            ReferidoTercero = x.ReferidoTercero, ReferenteId = x.ReferenteId,
            ModalidadCobro = x.ModalidadCobro, ObraSocialId = x.ObraSocialId,
            NumeroAutorizacion = x.NumeroAutorizacion, SesionesAutorizadas = x.SesionesAutorizadas,
            CicloObraSocialId = x.CicloObraSocialId, IniciarNuevoCicloObraSocial = x.IniciarNuevoCicloObraSocial,
            ConvenioCorroborado = x.ConvenioCorroborado, MedicoId = x.MedicoId,
            EsNuevoIngreso = x.EsNuevoIngreso, EsMonoxido = x.EsMonoxido,
            MonoxidoOrdenMedica = x.MonoxidoOrdenMedica, MonoxidoResumenClinico = x.MonoxidoResumenClinico,
            Camara = x.CamaraId.HasValue && cameras.TryGetValue(x.CamaraId.Value, out var found) ? found : null,
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
            ApartadoPorUserId = x.ApartadoPorUserId, ApartadoTs = x.ApartadoTs,
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
}