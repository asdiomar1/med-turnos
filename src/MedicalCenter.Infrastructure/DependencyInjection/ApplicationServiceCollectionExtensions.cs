using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Application.Features.Auth;
using MedicalCenter.Application.Features.ClinicalHistory;
using MedicalCenter.Application.Features.DailyClosings;
using MedicalCenter.Application.Features.Dashboards;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Application.Features.Catalogs;
using MedicalCenter.Application.Features.Configuration;
using MedicalCenter.Application.Features.Imports;
using MedicalCenter.Application.Features.OutOfHoursTurns;
using MedicalCenter.Application.Features.PatientNotes;
using MedicalCenter.Application.Features.Patients;
using MedicalCenter.Application.Features.Professionals;
using MedicalCenter.Application.Features.Rbac;
using MedicalCenter.Application.Features.Schedules;
using MedicalCenter.Application.Features.Staff;
using MedicalCenter.Application.Features.UserPreferences;
using MedicalCenter.Application.Features.Consultations;
using MedicalCenter.Application.Features.WhatsApp;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalCenter.Infrastructure.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPatientsService, PatientsService>();
        services.AddScoped<IProfessionalsService, ProfessionalsService>();
        services.AddScoped<ISchedulesService, SchedulesService>();
        services.AddScoped<IAppointmentsService, AppointmentsService>();
        services.AddScoped<IConsultationsService, ConsultationsService>();
        services.AddScoped<IOutOfHoursTurnsService, OutOfHoursTurnsService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDailyClosingsService, DailyClosingsService>();
        services.AddScoped<IAdminEventFeedService, AdminEventFeedService>();
        services.AddScoped<ICatalogsService, CatalogsService>();
        services.AddScoped<IWorkingDaysConfigService, WorkingDaysConfigService>();
        services.AddScoped<ICamposConfigService, CamposConfigService>();
        services.AddScoped<IWhatsappMessageSettingsService, WhatsappMessageSettingsService>();
        services.AddScoped<IWhatsappService, WhatsappService>();
        services.AddScoped<IWhatsappWebhookProcessor, WhatsappWebhookProcessor>();
        services.AddScoped<IImportPatientsService, ImportPatientsService>();
        services.AddScoped<IImportPatientsOrchestrator, ImportPatientsOrchestrator>();
        services.AddScoped<IPatientNotesService, PatientNotesService>();
        services.AddScoped<IClinicalHistoryService, ClinicalHistoryService>();
        services.AddScoped<IRbacService, RbacService>();
        return services;
    }
}
