using MedicalCenter.Api.Extensions;
using MedicalCenter.Infrastructure.DependencyInjection;
using MedicalCenter.Infrastructure.Options;
using MedicalCenter.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.ConfigureCredentialValidation();
builder.Services.AddApiConfiguration();
builder.Services.AddValidation();
builder.Services.AddSecurityEvents();
builder.Services.AddApiDocumentation();
builder.Services.AddCorsPolicies(builder.Configuration);
builder.Services.AddHealthCheckConfiguration();

var app = builder.Build();

app.UseApiMiddleware();
await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;