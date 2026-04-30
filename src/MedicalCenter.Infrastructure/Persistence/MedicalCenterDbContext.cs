using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence;

public sealed class MedicalCenterDbContext(DbContextOptions<MedicalCenterDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientNote> PatientNotes => Set<PatientNote>();
    public DbSet<ClinicalHistory> ClinicalHistories => Set<ClinicalHistory>();
    public DbSet<ClinicalEvolution> ClinicalEvolutions => Set<ClinicalEvolution>();
    public DbSet<PortalAccessToken> PortalAccessTokens => Set<PortalAccessToken>();
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Referente> Referentes => Set<Referente>();
    public DbSet<CampoConfig> CamposConfig => Set<CampoConfig>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<ScheduleHour> ScheduleHours => Set<ScheduleHour>();
    public DbSet<ConsultationScheduleHour> ConsultationScheduleHours => Set<ConsultationScheduleHour>();
    public DbSet<ConsultationSlot> ConsultationSlots => Set<ConsultationSlot>();
    public DbSet<ConsultationSession> ConsultationSessions => Set<ConsultationSession>();
    public DbSet<OutOfHoursTurn> OutOfHoursTurns => Set<OutOfHoursTurn>();
    public DbSet<BlockHistory> BlockHistories => Set<BlockHistory>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<DailyClosing> DailyClosings => Set<DailyClosing>();
    public DbSet<AdminEventFeedEntry> AdminEventFeedEntries => Set<AdminEventFeedEntry>();
    public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
    public DbSet<CondicionIva> CondicionesIva => Set<CondicionIva>();
    public DbSet<DiasLaborablesConfig> DiasLaborablesConfigs => Set<DiasLaborablesConfig>();
    public DbSet<WhatsappMessageSetting> WhatsappMessageSettings => Set<WhatsappMessageSetting>();
    public DbSet<WhatsappDispatchQueueItem> WhatsappDispatchQueueItems => Set<WhatsappDispatchQueueItem>();
    public DbSet<WhatsappMessage> WhatsappMessages => Set<WhatsappMessage>();
    public DbSet<WhatsappMessageAction> WhatsappMessageActions => Set<WhatsappMessageAction>();
    public DbSet<WhatsappTemplate> WhatsappTemplates => Set<WhatsappTemplate>();
    public DbSet<WhatsappWebhookEvent> WhatsappWebhookEvents => Set<WhatsappWebhookEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OperationRequest> OperationRequests => Set<OperationRequest>();
    public DbSet<Importacion> Importaciones => Set<Importacion>();
    public DbSet<ImportacionError> ImportacionErrors => Set<ImportacionError>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedicalCenterDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
