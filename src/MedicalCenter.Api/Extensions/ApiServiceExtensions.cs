using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using MedicalCenter.Api.Filters;
using MedicalCenter.Api.Middleware;
using MedicalCenter.Api.ModelBinding;
using MedicalCenter.Contracts.Validation.Auth;
using MedicalCenter.Infrastructure.Options;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Security;
using MedicalCenter.Infrastructure.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace MedicalCenter.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "FrontendCors";

    public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.AddService<GlobalAuthorizeFilter>();
            options.ValueProviderFactories.Add(new IdempotencyKeyValueProviderFactory());
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        });

        services.AddScoped<GlobalAuthorizeFilter>();
        services.AddScoped<OwnershipFilter>();

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        return services;
    }

    public static IServiceCollection AddSecurityEvents(this IServiceCollection services)
    {
        services.AddSingleton(Channel.CreateUnbounded<SecurityEvent>());
        services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();
        services.AddHostedService<SecurityAuditBackgroundService>();
        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    public static IServiceCollection AddCorsPolicies(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://127.0.0.1:5173", "http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddHealthCheckConfiguration(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<MedicalCenterDbContext>();
        return services;
    }

    public static WebApplicationBuilder ConfigureCredentialValidation(this WebApplicationBuilder builder)
    {
        var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("CredentialValidation");
        var isProduction = !builder.Environment.IsDevelopment();
        var issues = CollectCredentialIssues(builder.Configuration);
        LogCredentialIssues(issues, isProduction, logger);

        return builder;
    }

    private static List<string> CollectCredentialIssues(IConfiguration configuration)
    {
        var issues = new List<string>();

        ValidateJwtOptions(configuration, issues);
        ValidateApiKeyOptions(configuration, issues);
        ValidateR2Options(configuration, issues);
        ValidateWhatsAppOptions(configuration, issues);

        return issues;
    }

    private static void ValidateJwtOptions(IConfiguration configuration, List<string> issues)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) ||
            jwtOptions.SecretKey.Length < 32 ||
            jwtOptions.SecretKey.Contains("change-this-secret", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("JWT SecretKey is weak, empty, or uses default value (minimum 32 chars required)");
        }
    }

    private static void ValidateApiKeyOptions(IConfiguration configuration, List<string> issues)
    {
        var apiKeyOptions = configuration.GetSection(ApiKeyOptions.SectionName).Get<ApiKeyOptions>() ?? new ApiKeyOptions();
        if (string.IsNullOrWhiteSpace(apiKeyOptions.Key) || apiKeyOptions.Key.Contains("change-this", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("API Key is missing or uses default value");
        }
    }

    private static void ValidateR2Options(IConfiguration configuration, List<string> issues)
    {
        var r2Options = configuration.GetSection(R2Options.SectionName).Get<R2Options>() ?? new R2Options();

        AddIssueIfMissing(r2Options.AccountId, "R2 AccountId is missing or not configured", issues);
        AddIssueIfMissing(r2Options.AccessKeyId, "R2 AccessKeyId is missing or not configured", issues);
        AddIssueIfMissing(r2Options.SecretAccessKey, "R2 SecretAccessKey is missing or not configured", issues);
    }

    private static void ValidateWhatsAppOptions(IConfiguration configuration, List<string> issues)
    {
        var whatsAppOptions = configuration.GetSection(WhatsAppOptions.SectionName).Get<WhatsAppOptions>() ?? new WhatsAppOptions();

        if (string.IsNullOrWhiteSpace(whatsAppOptions.WebhookVerifyToken) ||
            whatsAppOptions.WebhookVerifyToken.Contains("change-this", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("WhatsApp WebhookVerifyToken uses default value");
        }

        if (string.IsNullOrWhiteSpace(whatsAppOptions.DispatchInternalSecret) ||
            whatsAppOptions.DispatchInternalSecret.Contains("change-this", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("WhatsApp DispatchInternalSecret uses default value");
        }
    }

    private static void AddIssueIfMissing(string? value, string issue, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("set-via-env", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(issue);
        }
    }

    private static void LogCredentialIssues(IReadOnlyCollection<string> issues, bool isProduction, ILogger logger)
    {
        if (issues.Count == 0)
        {
            logger.LogInformation("Credential validation passed: all secrets configured");
            return;
        }

        foreach (var issue in issues)
        {
            if (isProduction)
            {
                logger.LogCritical("SECURITY: {Issue}", issue);
            }
            else
            {
                logger.LogWarning("DEV WARNING: {Issue}", issue);
            }
        }

        if (isProduction)
        {
            logger.LogCritical("SECURITY: Application starting with {Count} credential issue(s) in PRODUCTION mode. Fix immediately!", issues.Count);
            return;
        }

        logger.LogWarning("DEV WARNING: {Count} credential issue(s) detected. OK for development, fix before production!", issues.Count);
    }


}

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // Only use HTTPS redirection in production
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);
        app.UseRateLimiter();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health/ready");

        return app;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("SkipDatabaseInitialization"))
        {
            await DatabaseInitializer.InitializeAsync(app.Services, app.Lifetime.ApplicationStopping);
        }
    }
}
