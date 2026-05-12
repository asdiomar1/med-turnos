using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MedicalCenter.IntegrationTests.Api.V1;

public sealed class DashboardsControllerContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DashboardsControllerContractTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task GetResumen_ReturnsOnlyExpectedFields()
    {
        var token = CreateJwtToken();

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/dashboards/resumen?fecha=2099-02-16", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(payload.RootElement.TryGetProperty("pacientes_hoy", out var pacientesHoy));
        Assert.True(payload.RootElement.TryGetProperty("apartados_activos", out var apartadosActivos));
        Assert.Equal(0, pacientesHoy.GetInt32());
        Assert.Equal(0, apartadosActivos.GetInt32());
        Assert.Equal(2, payload.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public async Task GetOcupacion_ReturnsFlatArrayIncludingZeroOccupancyCameras()
    {
        var token = CreateJwtToken();
        var fecha = new DateOnly(2099, 3, 10);
        await SeedCameraAsync(9101, 24, "Multiplaza");
        await SeedCameraAsync(9102, 8, "Individual");
        await SeedOccupiedAppointmentAsync(fecha, new TimeOnly(9, 0), 9101);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/dashboards/ocupacion?fecha=2099-03-10", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, payload.RootElement.ValueKind);
        var rows = payload.RootElement.EnumerateArray().ToArray();
        Assert.Contains(rows, x => x.GetProperty("camara_id").GetInt32() == 9101 && x.GetProperty("ocupados").GetInt32() == 1);
        Assert.Contains(rows, x => x.GetProperty("camara_id").GetInt32() == 9102 && x.GetProperty("ocupados").GetInt32() == 0);
    }

    [Fact]
    public async Task GetAgenda_ReturnsOperationalRows()
    {
        var token = CreateJwtToken();
        var fecha = new DateOnly(2099, 3, 11);
        var patient = await SeedPatientAsync("Paciente Agenda");
        await SeedCameraAsync(9201, 24, "Multiplaza");
        await SeedReservedAppointmentAsync(fecha, new TimeOnly(9, 0), 1, 9201, patient.Id);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/dashboards/agenda?fecha=2099-03-11", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, payload.RootElement.ValueKind);
        var row = Assert.Single(payload.RootElement.EnumerateArray());
        Assert.Equal("09:00", row.GetProperty("hora").GetString());
        Assert.Equal(1, row.GetProperty("lugar").GetInt32());
        Assert.Equal(9201, row.GetProperty("camara_id").GetInt32());
        Assert.Equal("Multiplaza", row.GetProperty("camara_nombre").GetString());
        Assert.Equal("Paciente Agenda", row.GetProperty("nombre_paciente").GetString());
        Assert.Equal("particular", row.GetProperty("modalidad_cobro").GetString());
        Assert.False(row.GetProperty("es_nuevo_ingreso").GetBoolean());
        Assert.False(row.GetProperty("es_bloque_completo").GetBoolean());
        Assert.Equal("asignado", row.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task GetAlertas_ReturnsUiContractArray()
    {
        var token = CreateJwtToken();
        var fecha = new DateOnly(2099, 3, 12);
        await SeedCameraAsync(9301, 10, "Camara Alertas");
        await SeedHeldAppointmentAsync(fecha, new TimeOnly(10, 0), 1, 9301);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/dashboards/alertas?fecha=2099-03-12", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, payload.RootElement.ValueKind);
        var row = Assert.Single(payload.RootElement.EnumerateArray());
        Assert.True(row.TryGetProperty("tipo", out _));
        Assert.True(row.TryGetProperty("titulo", out _));
        Assert.True(row.TryGetProperty("descripcion", out _));
        Assert.True(row.TryGetProperty("target_tab", out _));
        Assert.Equal(4, row.EnumerateObject().Count());
    }

    [Fact]
    public async Task GetVolumenSemanal_Returns7DailyRowsWithOcupados()
    {
        var token = CreateJwtToken();
        var fecha = new DateOnly(2099, 3, 20);
        await SeedCameraAsync(9401, 24, "Camara Volumen");
        await SeedOccupiedAppointmentAsync(fecha.AddDays(-2), new TimeOnly(9, 0), 9401);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/dashboards/volumen-semanal?fecha=2099-03-20", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, payload.RootElement.ValueKind);
        var rows = payload.RootElement.EnumerateArray().ToArray();
        Assert.Equal(7, rows.Length);
        Assert.All(rows, row =>
        {
            Assert.True(row.TryGetProperty("fecha", out _));
            Assert.True(row.TryGetProperty("ocupados", out _));
            Assert.Equal(2, row.EnumerateObject().Count());
        });
    }

    [Theory]
    [InlineData("/api/v1/dashboards/resumen?fecha=2026-13-40")]
    [InlineData("/api/v1/dashboards/ocupacion?fecha=fecha-invalida")]
    [InlineData("/api/v1/dashboards/agenda?fecha=2026-99-99")]
    [InlineData("/api/v1/dashboards/alertas?fecha=2026-99-99")]
    [InlineData("/api/v1/dashboards/volumen-semanal?fecha=fecha-invalida")]
    public async Task GetEndpoints_WithInvalidFecha_Returns400(string url)
    {
        var token = CreateJwtToken();

        var response = await SendAuthorizedAsync(HttpMethod.Get, url, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private string CreateJwtToken()
    {
        var userId = Guid.NewGuid();
        var bearerOptions = _factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        var validationParameters = bearerOptions.TokenValidationParameters;
        var signingKey = validationParameters.IssuerSigningKey ?? validationParameters.IssuerSigningKeys.First();
        var issuer = validationParameters.ValidIssuer ?? string.Empty;
        var audience = validationParameters.ValidAudience ?? string.Empty;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("identifier", "viewer"),
            new("is_staff", bool.TrueString.ToLowerInvariant()),
            new("permission", "staff.read")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, string token)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        return await _client.SendAsync(request);
    }

    private async Task SeedCameraAsync(int cameraId, int capacity, string name)
    {
        await using var ctx = CreateDbContext();
        if (await ctx.Cameras.AnyAsync(x => x.Id == cameraId))
        {
            return;
        }

        ctx.Cameras.Add(new Camera(cameraId, name, capacity, true));
        await ctx.SaveChangesAsync();
    }

    private async Task SeedOccupiedAppointmentAsync(DateOnly fecha, TimeOnly hora, int cameraId)
    {
        await using var ctx = CreateDbContext();

        var scheduleId = Guid.NewGuid();
        ctx.Schedules.Add(new Schedule(scheduleId, fecha, hora, 1, $"agenda-{scheduleId:N}"));
        var appointment = new Appointment(Guid.NewGuid(), scheduleId, fecha, hora, 1, cameraId);
        appointment.Reserve(Guid.NewGuid());
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();
    }

    private async Task<Patient> SeedPatientAsync(string nombre)
    {
        await using var ctx = CreateDbContext();
        var patient = new Patient(
            Guid.NewGuid(),
            nombre,
            new PatientAdministrativeInfo("555111", $"DNI-{Guid.NewGuid():N}", null, 1),
            new PatientPortalInfo(false));
        ctx.Patients.Add(patient);
        await ctx.SaveChangesAsync();
        return patient;
    }

    private async Task SeedReservedAppointmentAsync(DateOnly fecha, TimeOnly hora, int lugar, int cameraId, Guid patientId)
    {
        await using var ctx = CreateDbContext();
        var scheduleId = Guid.NewGuid();
        ctx.Schedules.Add(new Schedule(scheduleId, fecha, hora, lugar, $"agenda-res-{scheduleId:N}"));
        var appointment = new Appointment(Guid.NewGuid(), scheduleId, fecha, hora, lugar, cameraId);
        appointment.Reserve(patientId);
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();
    }

    private async Task SeedHeldAppointmentAsync(DateOnly fecha, TimeOnly hora, int lugar, int cameraId)
    {
        await using var ctx = CreateDbContext();
        var scheduleId = Guid.NewGuid();
        ctx.Schedules.Add(new Schedule(scheduleId, fecha, hora, lugar, $"agenda-hold-{scheduleId:N}"));
        var appointment = new Appointment(Guid.NewGuid(), scheduleId, fecha, hora, lugar, cameraId);
        appointment.Hold(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();
    }

    private MedicalCenterDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

        return new MedicalCenterDbContext(options);
    }
}
