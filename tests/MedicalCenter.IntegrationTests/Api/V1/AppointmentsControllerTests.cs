using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using MedicalCenter.Contracts.Appointments;
using MedicalCenter.Contracts.Consultations;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MedicalCenter.IntegrationTests.Api.V1;

public sealed class AppointmentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetByDate_SinToken_Retorna401()
    {
        var response = await _client.GetAsync("/api/v1/turnos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByDate_SinFecha_Retorna200ConListaVacia()
    {
        var token = CreateJwtToken();

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/turnos", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(items);
        Assert.Empty(items!);
    }

    [Fact]
    public async Task GetByDate_ConDatos_Retorna200ConTurno()
    {
        var token = CreateJwtToken();
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var turno = await SeedOccupiedAppointmentAsync(patientId, new DateOnly(2099, 2, 16), new TimeOnly(9, 15));

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/turnos?fecha=2099-02-16", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal(turno.AppointmentId, items![0].Id);
        Assert.Equal("ocupado", items[0].Estado);
        Assert.Equal(turno.PatientId, items[0].PacienteId);
        Assert.NotNull(items[0].Camara);
        Assert.Equal(turno.CameraId, items[0].Camara!.Id);
    }

    [Fact]
    public async Task GetByRange_ConLimitesFueraDeRango_Retorna200ConPaginadoClampeado()
    {
        var token = CreateJwtToken();
        var date = new DateOnly(2099, 4, 18);
        var first = await SeedFreeAppointmentAsync(date, new TimeOnly(10, 30), 4111, 1);
        var second = await SeedFreeAppointmentAsync(date, new TimeOnly(11, 30), 4111, 2);

        var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/api/v1/turnos/rango?fecha_inicio=2099-04-18&fecha_fin=2099-04-18&offset=-5&limit=60000",
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedTurnoEnrichedApiResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Items.Length);
        Assert.Equal(2, body.Total);
        Assert.Contains(body.Items, x => x.Id == first.AppointmentId);
        Assert.Contains(body.Items, x => x.Id == second.AppointmentId);
    }

    [Fact]
    public async Task GetByRange_SinTurnos_RetornaListaVaciaYTotalCero()
    {
        var token = CreateJwtToken();

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/turnos/rango?fecha_inicio=2099-05-19&fecha_fin=2099-05-19&offset=0&limit=50", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedTurnoEnrichedApiResponse>();
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(0, body.Total);
    }

    [Fact]
    public async Task GetByFecha_ConDatos_Retorna200ConTurno()
    {
        var token = CreateJwtToken();
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var turno = await SeedOccupiedAppointmentAsync(patientId, new DateOnly(2099, 6, 20), new TimeOnly(11, 45));

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/turnos/fecha?fecha=2099-06-20", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<TurnoEnrichedApiResponse[]>();
        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal(turno.AppointmentId, items[0].Id);
        Assert.Equal("ocupado", items[0].Estado);
        Assert.Equal(turno.PatientId, items[0].PacienteId);
        Assert.NotNull(items[0].Camara);
        Assert.Equal(turno.CameraId, items[0].Camara!.Id);
    }

    [Fact]
    public async Task GetByFecha_SinTurnos_RetornaListaVacia()
    {
        var token = CreateJwtToken();

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/turnos/fecha?fecha=2099-03-17", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<TurnoEnrichedApiResponse[]>();
        Assert.NotNull(items);
        Assert.Empty(items!);
    }

    [Fact]
    public async Task GetActivosByPaciente_ConTurnoOcupado_Retorna200ConTurno()
    {
        var token = CreateJwtToken();
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var turno = await SeedOccupiedAppointmentAsync(patientId, new DateOnly(2099, 8, 22), new TimeOnly(13, 10));

        var response = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/turnos/pacientes/{patientId}/activos", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal(turno.AppointmentId, items[0].Id);
        Assert.Equal(patientId, items[0].PacienteId);
        Assert.Equal("ocupado", items[0].Estado);
    }

    [Fact]
    public async Task GetDisponiblesPortal_ConDatosValidos_Retorna200ConDisponibles()
    {
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        await SeedPortalUserAsync(userId, patientId);
        var token = CreateJwtToken(userId);

        var occupied = await SeedOccupiedAppointmentAsync(patientId, new DateOnly(2099, 8, 23), new TimeOnly(9, 0));
        var free = await SeedFreeAppointmentAsync(new DateOnly(2099, 8, 23), new TimeOnly(9, 30), occupied.CameraId, 2);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/turnos/disponibles-portal?fecha=2099-08-23", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(items);
        Assert.Equal(2, items!.Length);
        Assert.Contains(items, item => item.Id == occupied.AppointmentId && item.PacienteId == patientId);
        Assert.Contains(items, item => item.Id == free.AppointmentId && item.PacienteId is null);
    }

    [Fact]
    public async Task Generate_YRepair_ConCredencialesManage_RetornanTotales()
    {
        var token = CreateJwtToken(Guid.NewGuid(), "consultas.asignar");
        await SeedCameraAsync(4301, 2, true, "Camara 4301");
        await SeedScheduleHourAsync(430_100, "09:00", 430_100, true);

        var generateResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/turnos/generar",
            token,
            JsonContent.Create(new GenerateAppointmentsRequest { Fecha = new DateOnly(2099, 9, 23) }));

        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
        var generated = await generateResponse.Content.ReadFromJsonAsync<TotalResponse>();
        Assert.NotNull(generated);
        Assert.True(generated!.Total > 0);

        var repairResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/turnos/reparar",
            token,
            JsonContent.Create(new RepairConsultationsRangeRequest
            {
                FechaInicio = new DateOnly(2099, 9, 24),
                FechaFin = new DateOnly(2099, 9, 25)
            }));

        Assert.Equal(HttpStatusCode.OK, repairResponse.StatusCode);
        var repaired = await repairResponse.Content.ReadFromJsonAsync<TotalResponse>();
        Assert.NotNull(repaired);
        Assert.True(repaired!.Total > 0);
    }

    [Fact]
    public async Task Assign_Cancel_YUpdateOperative_ConDatosValidos_RetornanEstadosYHistorial()
    {
        var actorUserId = Guid.NewGuid();
        var token = CreateJwtToken(actorUserId, "turnos.asignar");
        await SeedStaffActorWithPermissionsAsync(actorUserId, "turnos.asignar");
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var source = await SeedFreeAppointmentAsync(new DateOnly(2099, 10, 26), new TimeOnly(14, 0), 4401, 1);

        var assignResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{source.AppointmentId}/asignaciones?idempotencyKey=assign-{source.AppointmentId:N}",
            token,
            JsonContent.Create(new AssignAppointmentRequest
            {
                PacienteId = patientId,
                EsTanda = false,
                ReferidoTercero = false,
                ConvenioCorroborado = true,
                EsNuevoIngreso = false,
                EsMonoxido = false,
                MonoxidoOrdenMedica = false,
                MonoxidoResumenClinico = false
            }));

        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var assigned = await assignResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(assigned);
        Assert.Equal(source.AppointmentId, assigned!.Id);
        Assert.Equal("ocupado", assigned.Estado);
        Assert.Equal(patientId, assigned.PacienteId);

        var operativeResponse = await SendAuthorizedAsync(
            HttpMethod.Patch,
            $"/api/v1/turnos/{source.AppointmentId}/datos-operativos",
            token,
            JsonContent.Create(new AppointmentOperativeRequest
            {
                ReferidoTercero = true,
                ReferenteId = 77,
                ModalidadCobro = "obra_social",
                ObraSocialId = 33,
                NumeroAutorizacion = "OP-123",
                SesionesAutorizadas = 4,
                IniciarNuevoCicloObraSocial = true,
                ConvenioCorroborado = true,
                MedicoId = 22,
                MedicoUserId = Guid.NewGuid(),
                EsNuevoIngreso = true,
                EsMonoxido = true,
                MonoxidoOrdenMedica = true,
                MonoxidoResumenClinico = false
            }));

        Assert.Equal(HttpStatusCode.OK, operativeResponse.StatusCode);
        var operative = await operativeResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(operative);
        Assert.True(operative!.ReferidoTercero);
        Assert.Equal(77, operative.ReferenteId);
        Assert.Equal("obra_social", operative.ModalidadCobro);
        Assert.Equal(33, operative.ObraSocialId);
        Assert.Equal("OP-123", operative.NumeroAutorizacion);
        Assert.Equal(4, operative.SesionesAutorizadas);
        Assert.True(operative.IniciarNuevoCicloObraSocial);
        Assert.True(operative.ConvenioCorroborado);
        Assert.Equal(22, operative.MedicoId);
        Assert.True(operative.EsNuevoIngreso);
        Assert.True(operative.EsMonoxido);

        var cancelResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{source.AppointmentId}/cancelaciones?idempotencyKey=cancel-{source.AppointmentId:N}",
            token,
            JsonContent.Create(new CancelAppointmentRequest { Motivo = "Cambio de agenda" }));

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(cancelled);
        Assert.Equal("cancelado", cancelled!.Estado);
        Assert.Null(cancelled.PacienteId);

        await using var ctx = CreateDbContext();
        var persisted = await ctx.Appointments.SingleAsync(x => x.Id == source.AppointmentId);
        Assert.Equal("cancelado", persisted.Status.ToString().ToLowerInvariant());

        var historyResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/turnos/bloques/historial/slot/{source.AppointmentId}", token);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var history = await historyResponse.Content.ReadFromJsonAsync<BlockHistoryApiResponse[]>();
        Assert.NotNull(history);
        Assert.NotEmpty(history!);
    }

    [Fact]
    public async Task ApartarConfirmarLiberar_ConDatosValidos_RetornanEstadosEsperados()
    {
        var token = CreateJwtToken();
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var holdSlot = await SeedFreeAppointmentAsync(new DateOnly(2099, 11, 27), new TimeOnly(15, 0), 4501, 1);
        var releaseSlot = await SeedFreeAppointmentAsync(new DateOnly(2099, 11, 27), new TimeOnly(15, 30), 4501, 2);

        var holdResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{holdSlot.AppointmentId}/apartados?idempotencyKey=hold-{holdSlot.AppointmentId:N}",
            token,
            JsonContent.Create(new HoldAppointmentRequest
            {
                PacienteId = patientId,
                EsMonoxido = false,
                ReferidoTercero = false,
                ConvenioCorroborado = true,
                EsNuevoIngreso = false,
                MonoxidoOrdenMedica = false,
                MonoxidoResumenClinico = false
            }));

        Assert.Equal(HttpStatusCode.OK, holdResponse.StatusCode);
        var held = await holdResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(held);
        Assert.Equal("apartado", held!.Estado);
        Assert.Equal(patientId, held.PacienteId);

        var confirmResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{holdSlot.AppointmentId}/apartados/confirmaciones?idempotencyKey=confirm-{holdSlot.AppointmentId:N}",
            token,
            JsonContent.Create(new ConfirmHeldAppointmentRequest
            {
                PacienteId = patientId,
                EsMonoxido = false,
                ReferidoTercero = false,
                ConvenioCorroborado = true,
                EsNuevoIngreso = false,
                MonoxidoOrdenMedica = false,
                MonoxidoResumenClinico = false
            }));

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(confirmed);
        Assert.Equal("ocupado", confirmed!.Estado);
        Assert.Equal(patientId, confirmed.PacienteId);

        var releaseHoldResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{releaseSlot.AppointmentId}/apartados?idempotencyKey=hold-{releaseSlot.AppointmentId:N}",
            token,
            JsonContent.Create(new HoldAppointmentRequest
            {
                PacienteId = patientId,
                EsMonoxido = false,
                ReferidoTercero = false,
                ConvenioCorroborado = true,
                EsNuevoIngreso = false,
                MonoxidoOrdenMedica = false,
                MonoxidoResumenClinico = false
            }));

        Assert.Equal(HttpStatusCode.OK, releaseHoldResponse.StatusCode);

        var releaseResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{releaseSlot.AppointmentId}/apartados/liberaciones?idempotencyKey=release-{releaseSlot.AppointmentId:N}",
            token,
            JsonContent.Create(new ReleaseHeldAppointmentRequest { Motivo = "Libre nuevamente" }));

        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);
        var released = await releaseResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(released);
        Assert.Equal("libre", released!.Estado);
        Assert.Null(released.PacienteId);
    }

    [Fact]
    public async Task BloqueYTanda_ConDatosValidos_RetornanSlotsDisponibilidadHistorialYCancelacion()
    {
        var actorUserId = Guid.NewGuid();
        var token = CreateJwtToken(actorUserId, "turnos.asignar", "turnos.tanda");
        await SeedStaffActorWithPermissionsAsync(actorUserId, "turnos.asignar", "turnos.tanda");
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var fecha = new DateOnly(2099, 12, 28);
        var hora = new TimeOnly(16, 0);
        var cameraId = 4601;
        var slotA = await SeedFreeAppointmentAsync(fecha, hora, cameraId, 1);
        var slotB = await SeedFreeAppointmentAsync(fecha, hora, cameraId, 2);

        var blockResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/turnos/bloques/asignaciones?idempotencyKey=block-4601",
            token,
            JsonContent.Create(new AssignBlockAppointmentsRequest
            {
                Fecha = fecha,
                Hora = hora,
                CamaraId = cameraId,
                PacienteId = patientId,
                EsTanda = true,
                ReferidoTercero = false,
                ConvenioCorroborado = true,
                EsNuevoIngreso = true,
                EsMonoxido = false,
                MonoxidoOrdenMedica = false,
                MonoxidoResumenClinico = false
            }));

        Assert.Equal(HttpStatusCode.OK, blockResponse.StatusCode);

        var blockItems = await blockResponse.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(blockItems);
        Assert.Equal(2, blockItems!.Length);
        Assert.All(blockItems, item =>
        {
            Assert.Equal("ocupado", item.Estado);
            Assert.Equal(patientId, item.PacienteId);
            Assert.True(item.EsTanda);
            Assert.Equal(blockItems[0].TandaId, item.TandaId);
        });

        var tandaId = blockItems[0].TandaId;
        Assert.NotNull(tandaId);

        var disponibilidadResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/turnos/tandas/disponibilidad?fecha_inicio={fecha:yyyy-MM-dd}&fecha_fin={fecha:yyyy-MM-dd}&paciente_id={patientId}",
            token);

        Assert.Equal(HttpStatusCode.OK, disponibilidadResponse.StatusCode);
        var disponibilidad = await disponibilidadResponse.Content.ReadFromJsonAsync<TandaAvailabilityResponse[]>();
        Assert.NotNull(disponibilidad);
        Assert.Single(disponibilidad!);
        Assert.Equal(fecha, disponibilidad[0].Fecha);

        var detalleResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/turnos/tandas/disponibilidad/detalle?fecha_inicio={fecha:yyyy-MM-dd}&fecha_fin={fecha:yyyy-MM-dd}&paciente_id={patientId}",
            token);

        Assert.Equal(HttpStatusCode.OK, detalleResponse.StatusCode);
        var detalle = await detalleResponse.Content.ReadFromJsonAsync<TandaAvailabilityAggregatedApiResponse[]>();
        Assert.NotNull(detalle);
        Assert.Contains(detalle!, x => x.CamaraId == cameraId);

        var slotsResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/turnos/tandas/{tandaId}/slots", token);
        Assert.Equal(HttpStatusCode.OK, slotsResponse.StatusCode);
        var slots = await slotsResponse.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(slots);
        Assert.Equal(2, slots!.Length);

        var activeSlotsResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/turnos/tandas/{tandaId}/slots/activos", token);
        Assert.Equal(HttpStatusCode.OK, activeSlotsResponse.StatusCode);
        var activeSlots = await activeSlotsResponse.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(activeSlots);
        Assert.Equal(2, activeSlots!.Length);

        var updateTandaResponse = await SendAuthorizedAsync(
            HttpMethod.Patch,
            $"/api/v1/turnos/tandas/{tandaId}/datos-operativos",
            token,
            JsonContent.Create(new AppointmentOperativeRequest
            {
                ReferidoTercero = true,
                ReferenteId = 55,
                ModalidadCobro = "convenio",
                ObraSocialId = 77,
                NumeroAutorizacion = "AUTH-77",
                SesionesAutorizadas = 2,
                IniciarNuevoCicloObraSocial = false,
                ConvenioCorroborado = true,
                MedicoId = 88,
                EsNuevoIngreso = true,
                EsMonoxido = true,
                MonoxidoOrdenMedica = true,
                MonoxidoResumenClinico = true
            }));

        Assert.Equal(HttpStatusCode.OK, updateTandaResponse.StatusCode);
        var updatedTanda = await updateTandaResponse.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(updatedTanda);
        Assert.Equal(2, updatedTanda!.Length);
        Assert.All(updatedTanda, item => Assert.Equal("ocupado", item.Estado));

        var historyByRangeResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/turnos/bloques/historial/rango?fecha_inicio={fecha:yyyy-MM-dd}&fecha_fin={fecha:yyyy-MM-dd}&camara_id={cameraId}",
            token);

        Assert.Equal(HttpStatusCode.OK, historyByRangeResponse.StatusCode);
        var historyByRange = await historyByRangeResponse.Content.ReadFromJsonAsync<BlockHistoryApiResponse[]>();
        Assert.NotNull(historyByRange);
        Assert.NotEmpty(historyByRange!);

        var historyByDateResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/turnos/bloques/historial?fecha={fecha:yyyy-MM-dd}&hora={hora:HH:mm}&camara_id={cameraId}",
            token);

        Assert.Equal(HttpStatusCode.OK, historyByDateResponse.StatusCode);
        var historyByDate = await historyByDateResponse.Content.ReadFromJsonAsync<BlockHistoryApiResponse[]>();
        Assert.NotNull(historyByDate);
        Assert.NotEmpty(historyByDate!);

        var manageToken = CreateJwtToken(actorUserId, "consultas.asignar");

        var registerHistoryResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/turnos/bloques/historial",
            manageToken,
            JsonContent.Create(new[]
            {
                new RegisterBlockHistoryEntryRequest
                {
                    Fecha = fecha,
                    Hora = hora,
                    CamaraId = cameraId,
                    SlotId = slotA.AppointmentId,
                    Lugar = 1,
                    Accion = "auditoria",
                    PacienteId = patientId,
                    Motivo = "registro manual"
                }
            }));

        Assert.Equal(HttpStatusCode.OK, registerHistoryResponse.StatusCode);
        var registerTotal = await registerHistoryResponse.Content.ReadFromJsonAsync<TotalResponse>();
        Assert.NotNull(registerTotal);
        Assert.Equal(1, registerTotal!.Total);

        var historyBySlotResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/turnos/bloques/historial/slot/{slotA.AppointmentId}", token);
        Assert.Equal(HttpStatusCode.OK, historyBySlotResponse.StatusCode);
        var historyBySlot = await historyBySlotResponse.Content.ReadFromJsonAsync<BlockHistoryApiResponse[]>();
        Assert.NotNull(historyBySlot);
        Assert.NotEmpty(historyBySlot!);

        var cancelTandaResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/tandas/{tandaId}/cancelaciones?idempotencyKey=cancel-tanda-{tandaId:N}",
            token,
            JsonContent.Create(new CancelTandaRequest { Motivo = "Fin de jornada" }));

        Assert.Equal(HttpStatusCode.OK, cancelTandaResponse.StatusCode);
        var cancelledTanda = await cancelTandaResponse.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(cancelledTanda);
        Assert.Equal(2, cancelledTanda!.Length);
        Assert.All(cancelledTanda, item => Assert.Equal("cancelado", item.Estado));

        await using var ctx = CreateDbContext();
        var persistedA = await ctx.Appointments.SingleAsync(x => x.Id == slotA.AppointmentId);
        var persistedB = await ctx.Appointments.SingleAsync(x => x.Id == slotB.AppointmentId);
        Assert.Equal("cancelado", persistedA.Status.ToString().ToLowerInvariant());
        Assert.Equal("cancelado", persistedB.Status.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task CancelBlock_ConDatosValidos_Retorna200YCancelaBloque()
    {
        var token = CreateJwtToken();
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var fecha = new DateOnly(2099, 12, 30);
        var hora = new TimeOnly(18, 30);
        var cameraId = 4701;
        var slotA = await SeedFreeAppointmentAsync(fecha, hora, cameraId, 1);
        var slotB = await SeedFreeAppointmentAsync(fecha, hora, cameraId, 2);

        await using (var ctx = CreateDbContext())
        {
            var entityA = await ctx.Appointments.SingleAsync(x => x.Id == slotA.AppointmentId);
            var entityB = await ctx.Appointments.SingleAsync(x => x.Id == slotB.AppointmentId);
            entityA.Reserve(patientId);
            entityB.Reserve(patientId);
            ctx.Entry(entityA).Property(nameof(Appointment.EsBloqueCompleto)).CurrentValue = true;
            ctx.Entry(entityB).Property(nameof(Appointment.EsBloqueCompleto)).CurrentValue = true;
            await ctx.SaveChangesAsync();
        }

        var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/turnos/bloques/cancelaciones?idempotencyKey=cancel-block-4701",
            token,
            JsonContent.Create(new CancelBlockAppointmentsRequest
            {
                Fecha = fecha,
                Hora = hora,
                CamaraId = cameraId,
                PacienteId = patientId,
                Motivo = "Cambio de agenda"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cancelled = await response.Content.ReadFromJsonAsync<AppointmentApiResponse[]>();
        Assert.NotNull(cancelled);
        Assert.Equal(2, cancelled!.Length);
        Assert.All(cancelled, item => Assert.Equal("cancelado", item.Estado));
    }

    [Fact]
    public async Task RescheduleTanda_YRescheduleBlock_ConSlotSimple_Retornan200()
    {
        var token = CreateJwtToken();
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var source = await SeedOccupiedAppointmentAsync(patientId, new DateOnly(2099, 12, 29), new TimeOnly(17, 0));
        var target = await SeedFreeAppointmentAsync(new DateOnly(2099, 12, 29), new TimeOnly(17, 30), source.CameraId, 2);

        var rescheduleTandaResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{source.AppointmentId}/reprogramaciones/tanda?idempotencyKey=reschedule-tanda-{source.AppointmentId:N}",
            token,
            JsonContent.Create(new RescheduleAppointmentRequest
            {
                TargetSlotId = target.AppointmentId,
                Scope = "tanda"
            }));

        Assert.Equal(HttpStatusCode.OK, rescheduleTandaResponse.StatusCode);
        var rescheduledTanda = await rescheduleTandaResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(rescheduledTanda);
        Assert.Equal(target.AppointmentId, rescheduledTanda!.Id);
        Assert.Equal(patientId, rescheduledTanda.PacienteId);

        var sourceBlock = await SeedOccupiedAppointmentAsync(patientId, new DateOnly(2099, 12, 30), new TimeOnly(18, 0));
        var targetBlock = await SeedFreeAppointmentAsync(new DateOnly(2099, 12, 30), new TimeOnly(18, 30), sourceBlock.CameraId, 2);

        var rescheduleBlockResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{sourceBlock.AppointmentId}/reprogramaciones/bloque?idempotencyKey=reschedule-block-{sourceBlock.AppointmentId:N}",
            token,
            JsonContent.Create(new RescheduleAppointmentRequest
            {
                TargetSlotId = targetBlock.AppointmentId,
                Scope = "bloque_tanda"
            }));

        Assert.Equal(HttpStatusCode.OK, rescheduleBlockResponse.StatusCode);
        var rescheduledBlock = await rescheduleBlockResponse.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(rescheduledBlock);
        Assert.Equal(targetBlock.AppointmentId, rescheduledBlock!.Id);
        Assert.Equal(patientId, rescheduledBlock.PacienteId);
    }

    [Fact]
    public async Task Reschedule_ConScopeNormal_YDatosValidos_Retorna200()
    {
        var token = CreateJwtToken();
        var patientId = Guid.NewGuid();
        await SeedPatientAsync(patientId);
        var source = await SeedOccupiedAppointmentAsync(patientId, new DateOnly(2099, 12, 31), new TimeOnly(19, 0));
        var target = await SeedFreeAppointmentAsync(new DateOnly(2099, 12, 31), new TimeOnly(19, 30), source.CameraId, 2);

        var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/turnos/{source.AppointmentId}/reprogramaciones?idempotencyKey=reschedule-{source.AppointmentId:N}",
            token,
            JsonContent.Create(new RescheduleAppointmentRequest
            {
                TargetSlotId = target.AppointmentId,
                Scope = "normal"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rescheduled = await response.Content.ReadFromJsonAsync<AppointmentApiResponse>();
        Assert.NotNull(rescheduled);
        Assert.Equal(target.AppointmentId, rescheduled!.Id);
        Assert.Equal(patientId, rescheduled.PacienteId);
    }

    [Fact]
    public async Task Generate_ConTokenSinPolicy_Retorna403()
    {
        var token = CreateJwtToken();
        var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/turnos/generar",
            token,
            JsonContent.Create(new GenerateAppointmentsRequest
            {
                Fecha = new DateOnly(2099, 1, 15)
            }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private string CreateJwtToken() => CreateJwtToken(Guid.NewGuid());

    private string CreateJwtToken(Guid userId, params string[] permissions)
    {
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
            new("is_staff", bool.TrueString.ToLowerInvariant())
        };

        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        ValidateJwt(tokenString);

        return tokenString;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, string token, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        return await _client.SendAsync(request);
    }

    private async Task SeedPatientAsync(Guid patientId)
    {
        await using var ctx = CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();

        ctx.Patients.Add(new Patient(
            patientId,
            "Paciente de prueba",
            new PatientAdministrativeInfo("1155550000", $"DNI{patientId:N}", null, 1),
            new PatientPortalInfo(false)));

        await ctx.SaveChangesAsync();
    }

    private async Task SeedStaffUserAsync(Guid userId)
    {
        await using var ctx = CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();

        ctx.Users.Add(new User(new UserCreateParams(userId, $"viewer-{userId:N}", $"viewer-{userId:N}@medicalcenter.local", "hashed-password", true, true)));

        await ctx.SaveChangesAsync();
    }

    private async Task SeedPortalUserAsync(Guid userId, Guid patientId)
    {
        await using var ctx = CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();

        if (!await ctx.Users.AnyAsync(x => x.Id == userId))
        {
            ctx.Users.Add(new User(new UserCreateParams(userId, $"portal-{userId:N}", $"portal-{userId:N}@medicalcenter.local", "hashed-password", true, false, patientId, $"Portal {userId:N}")));
            await ctx.SaveChangesAsync();
        }
    }

    private async Task SeedStaffActorWithPermissionsAsync(Guid userId, params string[] permissions)
    {
        await using var ctx = CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();

        if (!await ctx.Users.AnyAsync(x => x.Id == userId))
        {
            ctx.Users.Add(new User(new UserCreateParams(userId, $"actor-{userId:N}", $"actor-{userId:N}@medicalcenter.local", "hashed-password", true, true)));
            await ctx.SaveChangesAsync();
        }

        var profileId = Guid.NewGuid();
        await ctx.Database.ExecuteSqlRawAsync(
            """
            insert into public.perfiles (id, nombre, rol, auth_user_id, portal_habilitado, requiere_reset_portal, portal_login_email)
            values ({0}, {1}, 'admin', {2}, false, false, {3});
            """,
            [profileId, $"Actor {userId:N}", userId, $"actor-{userId:N}@medicalcenter.local"]);

        var roleSlug = $"test-role-{userId:N}";
        await ctx.Database.ExecuteSqlRawAsync(
            """
            insert into public.rbac_roles (slug, nombre, descripcion, activo, is_system, is_staff, default_home, created_at, updated_at)
            values ({0}, {1}, null, true, true, true, '/usuario', now(), now())
            on conflict (slug) do nothing;
            """,
            [roleSlug, $"Role {userId:N}"]);

        var roleId = await ctx.Database.SqlQueryRaw<long>(
                "select id as \"Value\" from public.rbac_roles where slug = {0}",
                roleSlug)
            .SingleAsync();

        foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await ctx.Database.ExecuteSqlRawAsync(
                """
                insert into public.rbac_permissions (key, nombre, descripcion, modulo, is_system, created_at, updated_at)
                values ({0}, {1}, {2}, 'turnos', true, now(), now())
                on conflict (key) do nothing;
                """,
                [permission, permission, permission]);

            var permissionId = await ctx.Database.SqlQueryRaw<long>(
                    "select id as \"Value\" from public.rbac_permissions where key = {0}",
                    permission)
                .SingleAsync();

            await ctx.Database.ExecuteSqlRawAsync(
                """
                insert into public.rbac_role_permissions (role_id, permission_id, granted, created_at)
                values ({0}, {1}, true, now())
                on conflict (role_id, permission_id) do nothing;
                """,
                [roleId, permissionId]);
        }

        await ctx.Database.ExecuteSqlRawAsync(
            """
            insert into public.rbac_user_roles (user_id, role_id, is_primary, assigned_by, assigned_at, expires_at)
            values ({0}, {1}, true, null, now(), null)
            on conflict (user_id, role_id) do nothing;
            """,
            [profileId, roleId]);
    }

    private async Task SeedCameraAsync(int cameraId, int capacity, bool active, string name)
    {
        await using var ctx = CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();

        if (await ctx.Cameras.AnyAsync(x => x.Id == cameraId))
        {
            return;
        }

        ctx.Cameras.Add(new Camera(cameraId, name, capacity, active));
        await ctx.SaveChangesAsync();
    }

    private async Task SeedScheduleHourAsync(int id, string hora, int orden, bool active)
    {
        await using var ctx = CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();

        if (await ctx.ScheduleHours.AnyAsync(x => x.Id == id || x.Hora == hora || x.Orden == orden))
        {
            return;
        }

        ctx.ScheduleHours.Add(new ScheduleHour(id, hora, orden, active));
        await ctx.SaveChangesAsync();
    }

    private async Task<SeededAppointment> SeedFreeAppointmentAsync(DateOnly fecha, TimeOnly hora, int cameraId, int lugar)
    {
        var scheduleId = Guid.NewGuid();

        await using var ctx = CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();

        if (!await ctx.Cameras.AnyAsync(x => x.Id == cameraId))
        {
            ctx.Cameras.Add(new Camera(cameraId, $"Camara {cameraId}", 4, true));
        }

        var scheduleHourId = fecha.Day * 10_000 + hora.Hour * 60 + hora.Minute;
        if (!await ctx.ScheduleHours.AnyAsync(x => x.Hora == hora.ToString("HH:mm") || x.Id == scheduleHourId || x.Orden == scheduleHourId))
        {
            ctx.ScheduleHours.Add(new ScheduleHour(scheduleHourId, hora.ToString("HH:mm"), scheduleHourId, true));
        }

        ctx.Schedules.Add(new Schedule(scheduleId, fecha, hora, lugar, $"agenda-{scheduleId:N}"));
        var appointment = new Appointment(Guid.NewGuid(), scheduleId, fecha, hora, lugar, cameraId);
        ctx.Appointments.Add(appointment);

        await ctx.SaveChangesAsync();

        return new SeededAppointment(appointment.Id, cameraId, scheduleId);
    }

    private async Task<SeededAppointment> SeedOccupiedAppointmentAsync(Guid patientId, DateOnly fecha, TimeOnly hora)
    {
        var appointment = await SeedFreeAppointmentAsync(fecha, hora, 4101 + fecha.Day, 1);

        await using var ctx = CreateDbContext();
        var entity = await ctx.Appointments.SingleAsync(x => x.Id == appointment.AppointmentId);
        entity.Reserve(patientId);
        await ctx.SaveChangesAsync();

        return appointment with { PatientId = patientId };
    }

    private MedicalCenterDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

        return new MedicalCenterDbContext(options);
    }

    private void ValidateJwt(string token)
    {
        var bearerOptions = _factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var validationParameters = bearerOptions.TokenValidationParameters.Clone();
        validationParameters.ValidateLifetime = true;
        validationParameters.ClockSkew = TimeSpan.Zero;

        new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
    }

    private sealed record SeededAppointment(Guid AppointmentId, int CameraId, Guid ScheduleId, Guid? PatientId = null);

    private sealed class TotalResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; init; }
    }

    private sealed class AppointmentApiResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("estado")]
        public string Estado { get; init; } = string.Empty;

        [JsonPropertyName("paciente_id")]
        public Guid? PacienteId { get; init; }

        [JsonPropertyName("camara")]
        public CameraApiResponse? Camara { get; init; }

        [JsonPropertyName("tanda_id")]
        public Guid? TandaId { get; init; }

        [JsonPropertyName("es_tanda")]
        public bool EsTanda { get; init; }

        [JsonPropertyName("referido_tercero")]
        public bool ReferidoTercero { get; init; }

        [JsonPropertyName("referente_id")]
        public int? ReferenteId { get; init; }

        [JsonPropertyName("modalidad_cobro")]
        public string? ModalidadCobro { get; init; }

        [JsonPropertyName("obra_social_id")]
        public int? ObraSocialId { get; init; }

        [JsonPropertyName("numero_autorizacion")]
        public string? NumeroAutorizacion { get; init; }

        [JsonPropertyName("sesiones_autorizadas")]
        public int? SesionesAutorizadas { get; init; }

        [JsonPropertyName("iniciar_nuevo_ciclo_obra_social")]
        public bool IniciarNuevoCicloObraSocial { get; init; }

        [JsonPropertyName("convenio_corroborado")]
        public bool ConvenioCorroborado { get; init; }

        [JsonPropertyName("medico_id")]
        public int? MedicoId { get; init; }

        [JsonPropertyName("es_nuevo_ingreso")]
        public bool EsNuevoIngreso { get; init; }

        [JsonPropertyName("es_monoxido")]
        public bool EsMonoxido { get; init; }

        [JsonPropertyName("monoxido_orden_medica")]
        public bool MonoxidoOrdenMedica { get; init; }

        [JsonPropertyName("monoxido_resumen_clinico")]
        public bool MonoxidoResumenClinico { get; init; }
    }

    private sealed class CameraApiResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }
    }

    private sealed class PagedTurnoEnrichedApiResponse
    {
        [JsonPropertyName("items")]
        public TurnoEnrichedApiResponse[] Items { get; init; } = [];

        [JsonPropertyName("total")]
        public int Total { get; init; }
    }

    private sealed class TurnoEnrichedApiResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("fecha")]
        public DateOnly Fecha { get; init; }

        [JsonPropertyName("estado")]
        public string Estado { get; init; } = string.Empty;

        [JsonPropertyName("paciente_id")]
        public Guid? PacienteId { get; init; }

        [JsonPropertyName("camara_id")]
        public int? CamaraId { get; init; }

        [JsonPropertyName("camara")]
        public CameraApiResponse? Camara { get; init; }

        [JsonPropertyName("es_tanda")]
        public bool EsTanda { get; init; }

        [JsonPropertyName("tanda_id")]
        public Guid? TandaId { get; init; }
    }

    private sealed class BlockHistoryApiResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }
    }

    private sealed class TandaAvailabilityAggregatedApiResponse
    {
        [JsonPropertyName("camara_id")]
        public int CamaraId { get; init; }
    }
}
