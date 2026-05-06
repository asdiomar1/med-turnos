using System.Text.Json;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.AdminEventFeed;
using MedicalCenter.Contracts.Appointments;
using MedicalCenter.Contracts.Auth;
using MedicalCenter.Contracts.Catalogs;
using MedicalCenter.Contracts.ClinicalHistory;
using MedicalCenter.Contracts.Common;
using MedicalCenter.Contracts.Configuration;
using MedicalCenter.Contracts.Consultations;
using MedicalCenter.Contracts.DailyClosings;
using MedicalCenter.Contracts.Dashboards;
using MedicalCenter.Contracts.Imports;
using MedicalCenter.Contracts.PatientNotes;
using MedicalCenter.Contracts.Patients;
using MedicalCenter.Contracts.Professionals;
using MedicalCenter.Contracts.Rbac;
using MedicalCenter.Contracts.Schedules;
using MedicalCenter.Contracts.Staff;
using MedicalCenter.Contracts.Users;
using MedicalCenter.Contracts.WhatsApp;

namespace MedicalCenter.Api.Mappings;

/// <summary>
/// Centralized DTO → Response mapping extensions.
/// Replaces all private static Map() methods scattered across controllers.
/// </summary>
public static class SimpleResponseMappings
{
    // Catalog mappings
    public static CondicionIvaResponse ToResponse(this CondicionIvaSummaryDto x) =>
        new() { Id = x.Id, Nombre = x.Nombre, Activo = x.Activo, Orden = x.Orden, CreatedAt = x.CreatedAt };

    public static ObraSocialResponse ToResponse(this ObraSocialSummaryDto x) =>
        new() { Id = x.Id, Nombre = x.Nombre, Activa = x.Activa, TieneConvenio = x.TieneConvenio, Orden = x.Orden, Abreviatura = x.Abreviatura, CreatedAt = x.CreatedAt };

    // Professional mappings
    public static MedicoResponse ToResponse(this MedicoSummaryDto x) =>
        new() { Id = x.Id, Nombre = x.Nombre };

    public static ReferenteResponse ToResponse(this ReferenteSummaryDto x) =>
        new() { Id = x.Id, Nombre = x.Nombre, Tipo = x.Tipo, Activo = x.Activo, Orden = x.Orden, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt };

    public static OperadorCamaraResponse ToResponse(this OperadorCamaraSummaryDto x) =>
        new() { Id = x.Id, Nombre = x.Nombre, IsActive = x.IsActive };

    // Schedule mappings
    public static CameraResponse ToResponse(this CameraSummary x) =>
        new() { Id = x.Id, Nombre = x.Nombre, Capacidad = x.Capacidad, Activa = x.Activa };

    public static ScheduleHourResponse ToResponse(this ScheduleHourSummary x) =>
        new() { Id = x.Id, Hora = x.Hora, Orden = x.Orden, Activo = x.Activo };

    // Clinical history mappings
    public static ClinicalHistoryResponse ToResponse(this ClinicalHistorySummary x) =>
        new()
        {
            PatientId = x.PatientId, Numero = x.Numero, Antecedentes = x.Antecedentes,
            Alergias = x.Alergias, MedicacionActual = x.MedicacionActual,
            ObservacionesRelevantes = x.ObservacionesRelevantes, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
        };

    public static ClinicalEvolutionResponse ToResponse(this ClinicalEvolutionSummary x) =>
        new()
        {
            Id = x.Id, PatientId = x.PatientId, ConsultaSlotId = x.ConsultaSlotId,
            MedicoId = x.MedicoId, MedicoUserId = x.MedicoUserId, AuthorProfileId = x.AuthorProfileId,
            FechaClinica = x.FechaClinica, Titulo = x.Titulo, Nota = x.Nota,
            DiagnosticoImpresion = x.DiagnosticoImpresion, Indicaciones = x.Indicaciones,
            CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
            MedicoNombre = x.MedicoNombre, MedicoActivo = x.MedicoActivo,
        };

    // Patient note mappings
    public static PatientNoteResponse ToResponse(this PatientNoteSummary x) =>
        new() { Id = x.Id, PatientId = x.PatientId, AuthorId = x.AuthorId, AuthorNombre = x.AuthorNombre, Message = x.Message, CreatedAt = x.CreatedAt };

    // User preferences mappings
    public static UserPreferencesResponse ToResponse(this UserPreferencesSummary x) =>
        new()
        {
            UserId = x.UserId, Theme = x.Theme,
            CustomColors = ParseJson(x.CustomColorsJson),
            TurnosLayout = x.TurnosLayout, FontScale = x.FontScale,
            CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
        };

    // Configuration mappings
    public static DiasLaborablesConfigResponse ToResponse(this DiasLaborablesConfigDto x) =>
        new() { Key = x.Key, DiasSemana = x.DiasSemana };

    public static WhatsappMessageSettingResponse ToResponse(this WhatsappMessageSettingDto x) =>
        new()
        {
            Key = x.Key, Label = x.Label, Description = x.Description,
            MessageText = x.MessageText, Active = x.Active,
            CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
        };

    public static CampoConfigResponse ToResponse(this CampoConfigSummaryDto x) =>
        new() { Id = x.Id, Nombre = x.Nombre, Tipo = x.Tipo, Orden = x.Orden, CreatedAt = x.CreatedAt };

    // Dashboard mappings
    public static DashboardSummaryResponse ToResponse(this DashboardSummaryDto x) =>
        new()
        {
            Fecha = x.Fecha, TotalTurnos = x.TotalTurnos, Libres = x.Libres,
            Ocupados = x.Ocupados, Apartados = x.Apartados, Cancelados = x.Cancelados,
            OcupacionPorcentaje = x.OcupacionPorcentaje, GeneradoEn = x.GeneradoEn,
        };

    public static DashboardOccupancyResponse ToResponse(this DashboardOccupancyDto x) =>
        new()
        {
            Fecha = x.Fecha, TotalTurnos = x.TotalTurnos, Libres = x.Libres,
            Ocupados = x.Ocupados, Apartados = x.Apartados, Cancelados = x.Cancelados,
            OcupacionPorcentaje = x.OcupacionPorcentaje,
            PorCamara = x.PorCamara.Select(c => c.ToResponse()).ToArray(),
        };

    public static DashboardOccupancyCameraResponse ToResponse(this DashboardOccupancyCameraDto x) =>
        new()
        {
            CameraId = x.CameraId, CameraName = x.CameraName,
            TotalTurnos = x.TotalTurnos, Libres = x.Libres,
            Ocupados = x.Ocupados, Apartados = x.Apartados, Cancelados = x.Cancelados,
        };

    public static DashboardAgendaBucketResponse ToResponse(this DashboardAgendaBucketDto x) =>
        new()
        {
            Fecha = x.Fecha, Hora = x.Hora, CameraId = x.CameraId, CameraName = x.CameraName,
            TotalTurnos = x.TotalTurnos, Libres = x.Libres,
            Ocupados = x.Ocupados, Apartados = x.Apartados, Cancelados = x.Cancelados,
        };

    public static DashboardAlertResponse ToResponse(this DashboardAlertDto x) =>
        new() { Code = x.Code, Message = x.Message, Severity = x.Severity, Count = x.Count };

    public static DashboardWeeklyVolumeItemResponse ToResponse(this DashboardWeeklyVolumeItemDto x) =>
        new()
        {
            Fecha = x.Fecha, TotalTurnos = x.TotalTurnos, Libres = x.Libres,
            Ocupados = x.Ocupados, Apartados = x.Apartados, Cancelados = x.Cancelados,
        };

    // Daily closing mappings
    public static DailyClosingPreviewResponse ToResponse(this DailyClosingPreviewDto x) =>
        new()
        {
            Fecha = x.Fecha, TotalTurnos = x.TotalTurnos, Libres = x.Libres,
            Ocupados = x.Ocupados, Apartados = x.Apartados, Cancelados = x.Cancelados,
            OcupacionPorcentaje = x.OcupacionPorcentaje, AptoParaCierre = x.AptoParaCierre,
            Alertas = x.Alertas.Select(a => a.ToResponse()).ToArray(),
            GeneradoEn = x.GeneradoEn,
        };

    public static DailyClosingResponse ToResponse(this DailyClosingSummaryDto x) =>
        new()
        {
            Id = x.Id, Fecha = x.Fecha, Estado = x.Estado,
            Detalles = ParseJson(x.DetallesJson),
            CreatedByUserId = x.CreatedByUserId, ConfirmedByUserId = x.ConfirmedByUserId,
            ReopenedByUserId = x.ReopenedByUserId, MotivoReapertura = x.MotivoReapertura,
            CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
            ConfirmedAt = x.ConfirmedAt, ReopenedAt = x.ReopenedAt,
        };

    // RBAC mappings
    public static RbacPermissionResponse ToResponse(this RbacPermissionSummary x) =>
        new() { Key = x.Key, Nombre = x.Nombre, Descripcion = x.Descripcion, Modulo = x.Modulo, IsSystem = x.IsSystem };

    public static RbacRoleResponse ToResponse(this RbacRoleSummary x) =>
        new()
        {
            Slug = x.Slug, Nombre = x.Nombre, Descripcion = x.Descripcion,
            Activo = x.Activo, IsSystem = x.IsSystem, IsStaff = x.IsStaff,
            DefaultHome = x.DefaultHome, Permissions = x.Permissions,
        };

    public static RbacStaffUserResponse ToResponse(this RbacStaffUserSummary x) =>
        new()
        {
            Id = x.Id, Nombre = x.Nombre, Email = x.Email,
            AuthUserId = x.AuthUserId, IsActive = x.IsActive,
            Roles = x.Roles, PrimaryRole = x.PrimaryRole,
        };

    // Staff mapping
    public static StaffProfileResponse ToResponse(this StaffProfileSummary x) =>
        new()
        {
            Id = x.Id, Identifier = x.Identifier,
            Email = x.Email ?? x.Identifier,
            Nombre = x.Nombre ?? x.Identifier,
            IsActive = x.IsActive, IsStaff = x.IsStaff, Roles = x.Roles,
        };

    // Auth mappings
    public static AuthSessionResponse ToResponse(this AuthResponse result) =>
        new()
        {
            Session = new AuthTokenResponse
            {
                AccessToken = result.Session.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.Session.ExpiresInSeconds,
            },
            User = new AuthUserResponse { Id = result.UserId, Email = result.Email },
        };

    public static PortalActivationResponse ToResponse(this PortalActivationResult result) =>
        new() { Ok = result.Ok, LoginIdentifier = result.LoginIdentifier };

    public static PortalRecoveryResponse ToResponse(this PortalRecoveryResult result) =>
        new() { Ok = result.Ok, NeedsManualSupport = result.NeedsManualSupport };

    public static PortalAccessTokenResponse ToResponse(this PortalAccessTokenResult result) =>
        new()
        {
            TokenId = result.TokenId, Purpose = result.Purpose,
            DeliveryChannel = result.DeliveryChannel, ExpiresAt = result.ExpiresAt,
            TokenPlain = result.TokenPlain,
        };

    public static EffectiveAccessResponse ToResponse(this EffectiveAccessResult result) =>
        new()
        {
            ProfileId = result.ProfileId, Roles = result.Roles,
            EffectivePermissions = result.EffectivePermissions,
            PrimaryRole = result.PrimaryRole, DefaultHome = result.DefaultHome,
            IsStaff = result.IsStaff,
        };

    // Admin event feed mappings
    public static AdminEventFeedItemResponse ToResponse(this AdminEventFeedItemDto x) =>
        new()
        {
            Id = x.Id, OccurredAt = x.OccurredAt, ActorUserId = x.ActorUserId,
            ActorLabel = x.ActorLabel, ActionCode = x.ActionCode, ActionFamily = x.ActionFamily,
            EntityType = x.EntityType, EntityId = x.EntityId, AgendaType = x.AgendaType,
            PacienteId = x.PacienteId, PacienteNombre = x.PacienteNombre,
            MedicoId = x.MedicoId, MedicoNombre = x.MedicoNombre,
            Title = x.Title, Summary = x.Summary,
            Metadata = ParseMetadata(x.MetadataJson),
        };

    public static AdminEventFeedFilterOptionsResponse ToResponse(this AdminEventFeedFilterOptionsDto x) =>
        new()
        {
            Actors = x.Actors.Select(a => new AdminEventFeedActorOptionResponse { Id = a.Id, Label = a.Label }).ToArray(),
            Actions = x.Actions.Select(a => new AdminEventFeedActionOptionResponse { Code = a.Code, Family = a.Family, Label = a.Label }).ToArray(),
        };

    // WhatsApp mappings
    public static WhatsappDispatchResponse ToResponse(this WhatsappDispatchResult x) =>
        new() { Requested = x.Requested, Found = x.Found };

    public static WhatsappReminderResponse ToResponse(this WhatsappReminderResult x) =>
        new() { FechaObjetivo = x.FechaObjetivo, Total = x.Total };

    public static ClinicalHistoryResumenResponse ToResumenResponse(this ClinicalHistoryNumeroSummary x) =>
        new() { PatientId = x.PatientId, Numero = x.Numero };

    /// <summary>
    /// Helper: safely parse JSON strings that may be null or empty.
    /// </summary>
    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    /// <summary>
    /// Helper: parse metadata JSON with fallback to empty object.
    /// </summary>
    private static JsonElement ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}