using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using MedicalCenter.Api.Filters;
using MedicalCenter.Api.Middleware;
using MedicalCenter.Contracts.Validation.Auth;
using MedicalCenter.Infrastructure.Options;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Security;
using MedicalCenter.Infrastructure.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
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

    public static WebApplicationBuilder ConfigureJwtValidation(this WebApplicationBuilder builder)
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (jwtOptions.SecretKey.Length < 32 || jwtOptions.SecretKey.Contains("change-this-secret"))
        {
            builder.Logging.AddConsole();
            var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger<Program>();
            logger.LogCritical("JWT SecretKey is weak or uses default value. Change it immediately in production!");
        }

        return builder;
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

        app.UseHttpsRedirection();

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