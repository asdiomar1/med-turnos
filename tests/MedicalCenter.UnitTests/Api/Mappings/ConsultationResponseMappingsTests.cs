using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Contracts.Consultations;

namespace MedicalCenter.UnitTests.Api.Mappings;

public sealed class ConsultationResponseMappingsTests
{
    [Fact]
    public void ToResponse_ConsultationScheduleHourSummary_MapsAllFields()
    {
        var dto = new ConsultationScheduleHourSummary(1, "10:00", true, 1, DateTimeOffset.UtcNow);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Hora, response.Hora);
        Assert.Equal(dto.Activo, response.Activo);
        Assert.Equal(dto.Orden, response.Orden);
        Assert.Equal(dto.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public void ToResponse_ConsultationSlotSummary_MapsAllFieldsAndNested()
    {
        var paciente = new GuidLookupSummary(Guid.NewGuid(), "Paciente");
        var medico = new IntLookupSummary(1, "Medico");
        var user = new GuidLookupSummary(Guid.NewGuid(), "User");
        
        var dto = new ConsultationSlotSummary(
            Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), new TimeOnly(10, 0), "OCUPADO",
            paciente.Id, medico.Id, Guid.NewGuid(), medico.Nombre, null, null,
            Guid.NewGuid(), DateTimeOffset.UtcNow, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            paciente, medico, user, user, user);

        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Fecha, response.Fecha);
        Assert.Equal(dto.Hora, response.Hora);
        Assert.Equal(dto.Estado, response.Estado);
        Assert.Equal(dto.PacienteId, response.PacienteId);
        Assert.Equal(dto.MedicoId, response.MedicoId);
        Assert.Equal(dto.MedicoNombre, response.MedicoNombre);
        Assert.NotNull(response.Paciente);
        Assert.Equal(paciente.Nombre, response.Paciente.Nombre);
        Assert.NotNull(response.Medico);
        Assert.Equal(medico.Nombre, response.Medico.Nombre);
        Assert.NotNull(response.MedicoUser);
        Assert.Equal(user.Nombre, response.MedicoUser.Nombre);
    }

    [Fact]
    public void ToResponse_BlockHistorySummary_MapsAllFields()
    {
        var dto = new BlockHistorySummary(
            Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), new TimeOnly(10, 0), 1, Guid.NewGuid(),
            1, "Accion", Guid.NewGuid(), Guid.NewGuid(), "Motivo", false, "Modalidad",
            1, "AUT-123", Guid.NewGuid(), DateTimeOffset.UtcNow,
            1, Guid.NewGuid(), "Medico", true, 1, Guid.NewGuid(),
            10, Guid.NewGuid(), DateTimeOffset.UtcNow,
            new GuidLookupSummary(Guid.NewGuid(), "Paciente"),
            new IntLookupSummary(1, "Medico"),
            new IntLookupSummary(1, "Referente"),
            new ObraSocialSummaryDto(1, "OSDE", true, true, 1, "OS", DateTimeOffset.UtcNow),
            new GuidLookupSummary(Guid.NewGuid(), "Perfil"),
            new GuidLookupSummary(Guid.NewGuid(), "ValidatorPerfil"));

        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Accion, response.Accion);
        Assert.Equal(dto.Motivo, response.Motivo);
        Assert.Equal(dto.NumeroAutorizacion, response.NumeroAutorizacion);
        Assert.NotNull(response.Paciente);
        Assert.Equal(dto.Paciente.Nombre, response.Paciente.Nombre);
        Assert.NotNull(response.Medico);
        Assert.Equal(dto.Medico.Nombre, response.Medico.Nombre);
        Assert.NotNull(response.ObraSocial);
        Assert.Equal(dto.ObraSocial.Nombre, response.ObraSocial.Nombre);
    }

    [Fact]
    public void ToResponse_BlockHistorySummary_WithNullNested_ReturnsNullNested()
    {
        var dto = new BlockHistorySummary(
            Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), new TimeOnly(10, 0), 1, Guid.NewGuid(),
            1, "Accion", null, Guid.NewGuid(), null, false, "Modalidad",
            null, null, null, null,
            null, null, null, false, null, null,
            null, null, DateTimeOffset.UtcNow,
            null, null, null, null, null, null);

        var response = dto.ToResponse();

        Assert.Null(response.Paciente);
        Assert.Null(response.Medico);
        Assert.Null(response.Referente);
        Assert.Null(response.ObraSocial);
        Assert.Null(response.RealizadoPorPerfil);
    }

    [Fact]
    public void ToResponse_ConsultationScheduleHourDeletionPreviewSummary_MapsAllFields()
    {
        var dto = new ConsultationScheduleHourDeletionPreviewSummary(1, "10:00", true, 5);
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Hora, response.Hora);
        Assert.Equal(dto.CanDelete, response.CanDelete);
        Assert.Equal(dto.FutureSlotsCount, response.FutureSlotsCount);
    }

    [Fact]
    public void ToResponse_ConsultationSessionSummary_MapsAllFields()
    {
        var dto = new ConsultationSessionSummary(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today),
            new TimeOnly(10, 0), 1, DateTimeOffset.UtcNow, "Modalidad", 1, Guid.NewGuid(),
            "AUT-123", 10, Guid.NewGuid());
        
        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.NumeroAutorizacion, response.NumeroAutorizacion);
        Assert.Equal(dto.SesionesAutorizadas, response.SesionesAutorizadas);
    }

    [Fact]
    public void ToResponse_OutOfHoursTurnSummary_MapsAllFields()
    {
        var paciente = new GuidLookupSummary(Guid.NewGuid(), "Paciente");
        var medico = new IntLookupSummary(1, "Medico");
        var user = new GuidLookupSummary(Guid.NewGuid(), "User");
        var operador = new GuidLookupSummary(Guid.NewGuid(), "Operador");

        var dto = new OutOfHoursTurnSummary(
            Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), new TimeOnly(10, 0), paciente.Id,
            "Notas", Guid.NewGuid(), operador.Id, DateTimeOffset.UtcNow, true,
            true, true, 1, Guid.NewGuid(),
            paciente, medico, user, operador);

        var response = dto.ToResponse();

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.Notas, response.Notas);
        Assert.Equal(dto.MonoxidoOrdenMedica, response.MonoxidoOrdenMedica);
        Assert.NotNull(response.Paciente);
        Assert.Equal(paciente.Nombre, response.Paciente.Nombre);
        Assert.NotNull(response.MonoxidoMedico);
        Assert.Equal(medico.Nombre, response.MonoxidoMedico.Nombre);
        Assert.NotNull(response.OperadorCamara);
        Assert.Equal(operador.Nombre, response.OperadorCamara.Nombre);
    }
}
