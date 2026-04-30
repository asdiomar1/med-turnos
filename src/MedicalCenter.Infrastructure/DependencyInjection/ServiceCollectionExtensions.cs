using System.Text;
using System.Threading.RateLimiting;
using Amazon.Runtime;
using Amazon.S3;
using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.FeatureFlags;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.Storage;
using MedicalCenter.Application.Abstractions.WhatsApp;
using MedicalCenter.Application.Features.Imports;
using MedicalCenter.Infrastructure.Auth;
using MedicalCenter.Infrastructure.Configuration;
using MedicalCenter.Infrastructure.Caching;
using MedicalCenter.Infrastructure.FeatureFlags;
using MedicalCenter.Infrastructure.Options;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Persistence.Repositories;
using MedicalCenter.Infrastructure.Storage;
using MedicalCenter.Infrastructure.WhatsApp;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MedicalCenter.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.Configure<R2Options>(configuration.GetSection(R2Options.SectionName));
        services.Configure<ImportsOptions>(configuration.GetSection(ImportsOptions.SectionName));
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddSingleton<IImportsOptions, ImportsOptionsAdapter>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        var rateLimitingOptions = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingOptions.AuthPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.AuthWindowSeconds)
                    }));

            options.AddPolicy("general", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingOptions.GeneralPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.GeneralWindowSeconds)
                    }));

            options.AddPolicy("webhook", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Request.Headers["X-Api-Key"].ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingOptions.WebhookPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.WebhookWindowSeconds)
                    }));
        });

        services.AddDbContext<MedicalCenterDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Redis distributed caching
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });

        // Cloudflare R2 (S3-compatible)
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var r2 = sp.GetRequiredService<IOptions<R2Options>>().Value;
            var credentials = new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = r2.Endpoint,
                ForcePathStyle = true,
                // R2 uses SigV4 — the region value is "auto" but SDK needs a valid string
                AuthenticationRegion = r2.Region == "auto" ? "us-east-1" : r2.Region,
            };
            return new AmazonS3Client(credentials, config);
        });
        services.AddScoped<IObjectStorage, R2ObjectStorage>();
        services.AddScoped<IXlsxRowReader, ClosedXmlRowReader>();
        services.AddScoped<IImportacionesRepository, ImportacionesRepository>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRbacAdminRepository, RbacAdminRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<ICameraRepository, CameraRepository>();
        services.AddScoped<IScheduleHourRepository, ScheduleHourRepository>();
        services.AddScoped<IPortalAccessTokenRepository, PortalAccessTokenRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IOutOfHoursTurnRepository, OutOfHoursTurnRepository>();
        services.AddScoped<IBlockHistoryRepository, BlockHistoryRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IDailyClosingRepository, DailyClosingRepository>();
        services.AddScoped<IAdminEventFeedRepository, AdminEventFeedRepository>();
        services.AddScoped<ICondicionIvaRepository, CondicionIvaRepository>();
        services.AddScoped<IObraSocialRepository, ObraSocialRepository>();
        services.AddScoped<IDiasLaborablesConfigRepository, DiasLaborablesConfigRepository>();
        services.AddScoped<ICampoConfigRepository, CampoConfigRepository>();
        services.AddScoped<IWhatsappMessageSettingsRepository, WhatsappMessageSettingsRepository>();
        services.AddScoped<IWhatsappDispatchQueueRepository, WhatsappDispatchQueueRepository>();
        services.AddScoped<IWhatsappMessageRepository, WhatsappMessageRepository>();
        services.AddScoped<IWhatsappMessageActionRepository, WhatsappMessageActionRepository>();
        services.AddScoped<IWhatsappTemplateRepository, WhatsappTemplateRepository>();
        services.AddScoped<IWhatsappWebhookEventRepository, WhatsappWebhookEventRepository>();
        services.AddScoped<IPatientNoteRepository, PatientNoteRepository>();
        services.AddScoped<IClinicalHistoryRepository, ClinicalHistoryRepository>();
        services.AddScoped<IMedicoRepository, MedicoRepository>();
        services.AddScoped<IReferenteRepository, ReferenteRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddHttpClient<IWhatsAppSender, WhatsAppSender>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminAccess", policy => policy.RequireClaim("permission", "app.admin_panel.access"));
            options.AddPolicy("RbacRead", policy => policy.RequireClaim("permission", "rbac.read"));
            options.AddPolicy("RbacManage", policy => policy.RequireClaim("permission", "rbac.manage"));
            options.AddPolicy("StaffRead", policy => policy.RequireClaim("permission", "staff.read"));
            options.AddPolicy("StaffManage", policy => policy.RequireClaim("permission", "staff.manage"));
            options.AddPolicy("PatientsRead", policy => policy.RequireClaim("permission", "pacientes.read"));
            options.AddPolicy("PatientsManage", policy => policy.RequireClaim("permission", "pacientes.manage"));
            options.AddPolicy("ActivityRead", policy => policy.RequireClaim("permission", "actividad.read"));
            options.AddPolicy("ConfigRead", policy => policy.RequireClaim("permission", "config.read"));
            options.AddPolicy("ConfigHorariosManage", policy => policy.RequireClaim("permission", "config.horarios.manage"));
            options.AddPolicy("ConfigCatalogsManage", policy => policy.RequireClaim("permission", "config.catalogos.manage"));
            options.AddPolicy("WhatsappManage", policy => policy.RequireClaim("permission", "config.whatsapp.manage"));
            options.AddPolicy("WhatsappDispatch", policy => policy.RequireClaim("permission", "whatsapp.dispatch"));
            options.AddPolicy("ClinicalHistoryRead", policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", "historia_clinica.resumen.read") ||
                ctx.User.HasClaim("permission", "historia_clinica.detalle.read") ||
                ctx.User.HasClaim("permission", "historia_clinica.editar_ficha") ||
                ctx.User.HasClaim("permission", "historia_clinica.crear_evolucion") ||
                ctx.User.HasClaim("permission", "historia_clinica.editar_numero")));
            options.AddPolicy("ClinicalHistoryManage", policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", "historia_clinica.editar_ficha") ||
                ctx.User.HasClaim("permission", "historia_clinica.crear_evolucion") ||
                ctx.User.HasClaim("permission", "historia_clinica.editar_numero")));
            options.AddPolicy("ConsultasRead", policy => policy.RequireClaim("permission", "consultas.read"));
            options.AddPolicy("ConsultasManage", policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", "consultas.asignar") ||
                ctx.User.HasClaim("permission", "consultas.cancelar") ||
                ctx.User.HasClaim("permission", "consultas.reprogramar") ||
                ctx.User.HasClaim("permission", "consultas.cerrar")));
            options.AddPolicy("TurnosFueraHorarioManage", policy => policy.RequireClaim("permission", "turnos.fuera_horario"));
        });

        return services;
    }
}