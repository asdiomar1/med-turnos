using System.Linq;
using System.Text.Json;
using MedicalCenter.Api.Mappings;
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

namespace MedicalCenter.UnitTests.Api.Mappings;

public sealed class SimpleResponseMappingsTests
{
    [Fact]
    public void ToResponse_CondicionIvaSummaryDto_MapsAllFields()
    {
        var dto = new CondicionIvaSummaryDto(1, "Responsable Inscripto", true, 10, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Activo, response.Activo);
        Assert.Equal(dto.Orden, response.Orden);
        Assert.Equal(dto.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public void ToResponse_ObraSocialSummaryDto_MapsAllFields()
    {
        var dto = new ObraSocialSummaryDto(1, "OSDE", true, true, 5, "OSDE", DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Abreviatura, response.Abreviatura);
        Assert.Equal(dto.Activa, response.Activa);
        Assert.Equal(dto.TieneConvenio, response.TieneConvenio);
        Assert.Equal(dto.Orden, response.Orden);
        Assert.Equal(dto.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public void ToResponse_MedicoSummaryDto_MapsAllFields()
    {
        var dto = new MedicoSummaryDto(Guid.NewGuid(), "Dr. House");
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Nombre, response.MedicoNombre);
    }

    [Fact]
    public void ToResponse_ReferenteSummaryDto_MapsAllFields()
    {
        var dto = new ReferenteSummaryDto(1, "Clinica Central", "Institución", true, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Tipo, response.Tipo);
        Assert.Equal(dto.Activo, response.Activo);
        Assert.Equal(dto.Orden, response.Orden);
        Assert.Equal(dto.CreatedAt, response.CreatedAt);
        Assert.Equal(dto.UpdatedAt, response.UpdatedAt);
    }

    [Fact]
    public void ToResponse_OperadorCamaraSummaryDto_MapsAllFields()
    {
        var dto = new OperadorCamaraSummaryDto(Guid.NewGuid(), "Juan Perez", true);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.IsActive, response.IsActive);
    }

    [Fact]
    public void ToResponse_DashboardSummaryDto_MapsAllFields()
    {
        var dto = new DashboardSummaryDto(DateOnly.FromDateTime(DateTime.Today), 100, 40, 30, 20, 10, 50.5m, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Fecha, response.Fecha);
        Assert.Equal(dto.TotalTurnos, response.TotalTurnos);
        Assert.Equal(dto.Libres, response.Libres);
        Assert.Equal(dto.Ocupados, response.Ocupados);
        Assert.Equal(dto.Apartados, response.Apartados);
        Assert.Equal(dto.Cancelados, response.Cancelados);
        Assert.Equal(dto.OcupacionPorcentaje, response.OcupacionPorcentaje);
        Assert.Equal(dto.GeneradoEn, response.GeneradoEn);
    }

    [Fact]
    public void ToResponse_DashboardOccupancyDto_MapsAllFields()
    {
        var cameraDto = new DashboardOccupancyCameraDto(1, "Camara 1", 50, 20, 15, 10, 5);
        var dto = new DashboardOccupancyDto(DateOnly.FromDateTime(DateTime.Today), 100, 40, 30, 20, 10, 50.5m, new[] { cameraDto });
        var response = dto.ToResponse();

        Assert.Equal(dto.Fecha, response.Fecha);
        Assert.Equal(dto.TotalTurnos, response.TotalTurnos);
        Assert.Equal(dto.Libres, response.Libres);
        Assert.Equal(dto.Ocupados, response.Ocupados);
        Assert.Equal(dto.Apartados, response.Apartados);
        Assert.Equal(dto.Cancelados, response.Cancelados);
        Assert.Equal(dto.OcupacionPorcentaje, response.OcupacionPorcentaje);
        Assert.Single(response.PorCamara);
        Assert.Equal(cameraDto.CameraId, response.PorCamara.First().CameraId);
        Assert.Equal(cameraDto.CameraName, response.PorCamara.First().CameraName);
    }

    [Fact]
    public void ToResponse_DailyClosingPreviewDto_MapsAllFields()
    {
        var alertDto = new DashboardAlertDto("ERR01", "Error message", "High", 1);
        var turnoDto = new DailyClosingTurnoDto(
            Guid.NewGuid(), null, null, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), "10:00", 1, "Camara 1", 1, "Paciente", 1, "Modalidad", 1, "OSDE", "OS", 100.0m, "AUT-123", 10, Guid.NewGuid(), true, 1, "Medico", false, false, true);
        var dto = new DailyClosingPreviewDto(
            DateOnly.FromDateTime(DateTime.Today), 10, 5, 3, 1, 1, 30.0m, true, 
            new[] { alertDto }, DateTimeOffset.UtcNow, new[] { turnoDto });
        
        var response = dto.ToResponse();

        Assert.Equal(dto.Fecha, response.Fecha);
        Assert.Equal(dto.TotalTurnos, response.TotalTurnos);
        Assert.Equal(dto.Libres, response.Libres);
        Assert.Equal(dto.Ocupados, response.Ocupados);
        Assert.Equal(dto.Apartados, response.Apartados);
        Assert.Equal(dto.Cancelados, response.Cancelados);
        Assert.Equal(dto.OcupacionPorcentaje, response.OcupacionPorcentaje);
        Assert.Equal(dto.AptoParaCierre, response.AptoParaCierre);
        Assert.Equal(dto.GeneradoEn, response.GeneradoEn);
        Assert.Single(response.Alertas);
        Assert.Single(response.Turnos);
        Assert.Equal(alertDto.Code, response.Alertas.First().Code);
        Assert.Equal(turnoDto.SlotId, response.Turnos.First().SlotId);
    }

    [Fact]
    public void ToResponse_DailyClosingSummaryDto_WithValidJson_ParsesJson()
    {
        var json = "{\"key\": \"value\"}";
        var dto = new DailyClosingSummaryDto(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), "Cerrado", json, Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow, null, null, null);
        
        var response = dto.ToResponse();

        Assert.NotNull(response.Detalles);
        Assert.Equal("value", response.Detalles.Value.GetProperty("key").GetString());
    }

    [Fact]
    public void ToResponse_DailyClosingSummaryDto_WithNullJson_ReturnsNullDetalles()
    {
        var dto = new DailyClosingSummaryDto(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), "Abierto", null, Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow, null, null, null);
        
        var response = dto.ToResponse();

        Assert.Null(response.Detalles);
    }

    [Fact]
    public void ToResponse_AdminEventFeedItemDto_MapsAllFields()
    {
        var metadataJson = "{\"meta\": 123}";
        var dto = new AdminEventFeedItemDto(
            1L, DateTimeOffset.UtcNow, Guid.NewGuid(), "Admin", "CODE", "FAMILY", "ENTITY", "ID", "Tanda", 
            Guid.NewGuid(), "Paciente", 1, "Medico", "Title", "Summary", metadataJson);
        
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.OccurredAt, response.OccurredAt);
        Assert.Equal(dto.ActorUserId, response.ActorUserId);
        Assert.Equal(dto.ActorLabel, response.ActorLabel);
        Assert.Equal(dto.ActionCode, response.ActionCode);
        Assert.Equal(dto.ActionFamily, response.ActionFamily);
        Assert.Equal(dto.EntityType, response.EntityType);
        Assert.Equal(dto.EntityId, response.EntityId);
        Assert.Equal(dto.AgendaType, response.AgendaType);
        Assert.Equal(dto.PacienteId, response.PacienteId);
        Assert.Equal(dto.PacienteNombre, response.PacienteNombre);
        Assert.Equal(dto.MedicoId, response.MedicoId);
        Assert.Equal(dto.MedicoNombre, response.MedicoNombre);
        Assert.Equal(dto.Title, response.Title);
        Assert.Equal(dto.Summary, response.Summary);
        Assert.Equal(123, response.Metadata.GetProperty("meta").GetInt32());
    }

    [Fact]
    public void ToResponse_RbacPermissionSummary_MapsAllFields()
    {
        var dto = new RbacPermissionSummary("perm.key", "Permission", "Description", "Module", true);
        var response = dto.ToResponse();

        Assert.Equal(dto.Key, response.Key);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Descripcion, response.Descripcion);
        Assert.Equal(dto.Modulo, response.Modulo);
        Assert.Equal(dto.IsSystem, response.IsSystem);
    }

    [Fact]
    public void ToResponse_RbacRoleSummary_MapsAllFields()
    {
        var dto = new RbacRoleSummary("admin", "Admin", "Admin role", true, true, true, "/home", new[] { "perm1" });
        var response = dto.ToResponse();

        Assert.Equal(dto.Slug, response.Slug);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Descripcion, response.Descripcion);
        Assert.Equal(dto.Activo, response.Activo);
        Assert.Equal(dto.IsSystem, response.IsSystem);
        Assert.Equal(dto.IsStaff, response.IsStaff);
        Assert.Equal(dto.DefaultHome, response.DefaultHome);
        Assert.Equal(dto.Permissions, response.Permissions);
    }

    [Fact]
    public void ToResponse_RbacStaffUserSummary_MapsAllFields()
    {
        var dto = new RbacStaffUserSummary(Guid.NewGuid(), "User", "user@test.com", Guid.NewGuid(), true, new[] { "admin" }, "admin");
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Email, response.Email);
        Assert.Equal(dto.AuthUserId, response.AuthUserId);
        Assert.Equal(dto.IsActive, response.IsActive);
        Assert.Equal(dto.Roles, response.Roles);
        Assert.Equal(dto.PrimaryRole, response.PrimaryRole);
    }

    [Fact]
    public void ToResponse_StaffProfileSummary_MapsAllFields()
    {
        var dto = new StaffProfileSummary(Guid.NewGuid(), "staff", "staff@test.com", "Staff", true, true, new[] { "staff" });
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Identifier, response.Identifier);
        Assert.Equal(dto.Email, response.Email);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.IsActive, response.IsActive);
        Assert.Equal(dto.IsStaff, response.IsStaff);
        Assert.Equal(dto.Roles, response.Roles);
    }

    [Fact]
    public void ToResponse_AuthResponse_MapsAllFields()
    {
        var result = new AuthResponse(new TokenEnvelope("token", "jwt", 3600), "refresh", Guid.NewGuid(), "test@test.com");
        var response = result.ToResponse();

        Assert.Equal(result.Session.AccessToken, response.Session.AccessToken);
        Assert.Equal(result.RefreshToken, response.Session.RefreshToken);
        Assert.Equal(result.Session.ExpiresInSeconds, response.Session.ExpiresIn);
        Assert.Equal(result.UserId, response.User.Id);
        Assert.Equal(result.Email, response.User.Email);
    }

    [Fact]
    public void ToResponse_PortalActivationResult_MapsAllFields()
    {
        var result = new PortalActivationResult(true, "user123");
        var response = result.ToResponse();

        Assert.True(response.Ok);
        Assert.Equal("user123", response.LoginIdentifier);
    }

    [Fact]
    public void ToResponse_PortalRecoveryResult_MapsAllFields()
    {
        var result = new PortalRecoveryResult(true, false);
        var response = result.ToResponse();

        Assert.True(response.Ok);
        Assert.False(response.NeedsManualSupport);
    }

    [Fact]
    public void ToResponse_PortalAccessTokenResult_MapsAllFields()
    {
        var result = new PortalAccessTokenResult(Guid.NewGuid(), "auth", "email", DateTimeOffset.UtcNow.AddHours(1), "plain");
        var response = result.ToResponse();

        Assert.Equal(result.TokenId, response.TokenId);
        Assert.Equal(result.Purpose, response.Purpose);
        Assert.Equal(result.DeliveryChannel, response.DeliveryChannel);
        Assert.Equal(result.ExpiresAt, response.ExpiresAt);
        Assert.Equal(result.TokenPlain, response.TokenPlain);
    }

    [Fact]
    public void ToResponse_EffectiveAccessResult_MapsAllFields()
    {
        var result = new EffectiveAccessResult(Guid.NewGuid(), new[] { "role" }, new[] { "perm" }, "role", "/home", true);
        var response = result.ToResponse();

        Assert.Equal(result.ProfileId, response.ProfileId);
        Assert.Equal(result.Roles, response.Roles);
        Assert.Equal(result.EffectivePermissions, response.EffectivePermissions);
        Assert.Equal(result.PrimaryRole, response.PrimaryRole);
        Assert.Equal(result.DefaultHome, response.DefaultHome);
        Assert.Equal(result.IsStaff, response.IsStaff);
    }

    [Fact]
    public void ToResponse_AdminEventFeedFilterOptionsDto_MapsAllFields()
    {
        var actor = new AdminEventFeedActorOptionDto(Guid.NewGuid(), "Actor");
        var action = new AdminEventFeedActionOptionDto("CODE", "FAMILY", "Label");
        var dto = new AdminEventFeedFilterOptionsDto(new[] { actor }, new[] { action });
        
        var response = dto.ToResponse();

        Assert.Single(response.Actors);
        Assert.Single(response.Actions);
        Assert.Equal(actor.Id, response.Actors.First().Id);
        Assert.Equal(action.Code, response.Actions.First().Code);
    }

    [Fact]
    public void ToResponse_WhatsappDispatchResult_MapsAllFields()
    {
        var result = new WhatsappDispatchResult(10, 5);
        var response = result.ToResponse();

        Assert.Equal(10, response.Requested);
        Assert.Equal(5, response.Found);
    }

    [Fact]
    public void ToResponse_WhatsappReminderResult_MapsAllFields()
    {
        var result = new WhatsappReminderResult(DateOnly.FromDateTime(DateTime.Today), 100);
        var response = result.ToResponse();

        Assert.Equal(result.FechaObjetivo, response.FechaObjetivo);
        Assert.Equal(100, response.Total);
    }

    [Fact]
    public void ToResponse_CameraSummary_MapsAllFields()
    {
        var dto = new CameraSummary(1, "Camara 1", 10, true);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
        Assert.Equal(dto.Capacidad, response.Capacidad);
        Assert.Equal(dto.Activa, response.Activa);
    }

    [Fact]
    public void ToResponse_ScheduleHourSummary_MapsAllFields()
    {
        var dto = new ScheduleHourSummary(1, "10:00", 1, true);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Hora, response.Hora);
        Assert.Equal(dto.Orden, response.Orden);
        Assert.Equal(dto.Activo, response.Activo);
    }

    [Fact]
    public void ToResponse_ClinicalHistorySummary_MapsAllFields()
    {
        var dto = new ClinicalHistorySummary(Guid.NewGuid(), 12345, "Ant", "Ale", "Med", "Obs", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.PatientId, response.PatientId);
        Assert.Equal(dto.Numero, response.Numero);
        Assert.Equal(dto.Antecedentes, response.Antecedentes);
        Assert.Equal(dto.Alergias, response.Alergias);
    }

    [Fact]
    public void ToResponse_ClinicalEvolutionSummary_MapsAllFields()
    {
        var dto = new ClinicalEvolutionSummary(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today), "Title", "Nota", "Diag", "Ind", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            "Medico", true);
        
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Titulo, response.Titulo);
        Assert.Equal(dto.MedicoNombre, response.MedicoNombre);
    }

    [Fact]
    public void ToResponse_PatientNoteSummary_MapsAllFields()
    {
        var dto = new PatientNoteSummary(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Message", DateTimeOffset.UtcNow, "Author");
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Message, response.Message);
        Assert.Equal(dto.AuthorNombre, response.AuthorNombre);
    }

    [Fact]
    public void ToResponse_UserPreferencesSummary_MapsAllFields()
    {
        var dto = new UserPreferencesSummary(Guid.NewGuid(), "dark", "{\"c\": 1}", "list", 1.0m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Theme, response.Theme);
        Assert.Equal(1, response.CustomColors.Value.GetProperty("c").GetInt32());
    }

    [Fact]
    public void ToResponse_DiasLaborablesConfigDto_MapsAllFields()
    {
        var dto = new DiasLaborablesConfigDto("key", new short[] { 1, 2 });
        var response = dto.ToResponse();

        Assert.Equal(dto.Key, response.Key);
        Assert.Equal(dto.DiasSemana, response.DiasSemana);
    }

    [Fact]
    public void ToResponse_WhatsappMessageSettingDto_MapsAllFields()
    {
        var dto = new WhatsappMessageSettingDto("key", "label", "desc", "text", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Key, response.Key);
        Assert.Equal(dto.MessageText, response.MessageText);
    }

    [Fact]
    public void ToResponse_CampoConfigSummaryDto_MapsAllFields()
    {
        var dto = new CampoConfigSummaryDto(Guid.NewGuid(), "Nombre", "Tipo", 1, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Nombre, response.Nombre);
    }

    [Fact]
    public void ToResponse_DashboardAgendaBucketDto_MapsAllFields()
    {
        var dto = new DashboardAgendaBucketDto(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(10, 0), 1, "Cam", 10, 5, 3, 1, 1);
        var response = dto.ToResponse();

        Assert.Equal(dto.Hora, response.Hora);
        Assert.Equal(dto.TotalTurnos, response.TotalTurnos);
    }

    [Fact]
    public void ToResponse_DashboardWeeklyVolumeItemDto_MapsAllFields()
    {
        var dto = new DashboardWeeklyVolumeItemDto(DateOnly.FromDateTime(DateTime.Today), 100, 50, 30, 10, 10);
        var response = dto.ToResponse();

        Assert.Equal(dto.Fecha, response.Fecha);
        Assert.Equal(dto.TotalTurnos, response.TotalTurnos);
    }

    [Fact]
    public void ToResumenResponse_ClinicalHistoryNumeroSummary_MapsAllFields()
    {
        var summary = new ClinicalHistoryNumeroSummary(Guid.NewGuid(), 12345);
        var response = summary.ToResumenResponse();

        Assert.Equal(summary.PatientId, response.PatientId);
        Assert.Equal(summary.Numero, response.Numero);
    }
}
