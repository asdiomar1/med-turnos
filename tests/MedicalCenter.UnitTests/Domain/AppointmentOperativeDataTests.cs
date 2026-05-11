using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Constants;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.UnitTests.Domain;

public sealed class AppointmentOperativeDataTests
{
    [Fact]
    public void Reserve_WithOperativeData_AppliesAllOperativeFields()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var patientId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();
        var cicloId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();

        var operative = new AppointmentOperativeData(
            ReferidoTercero: true,
            ReferenteId: 42,
            ModalidadCobro: "obra_social",
            ObraSocialId: 5,
            NumeroAutorizacion: "AUTH-12345",
            SesionesAutorizadas: 10,
            CicloObraSocialId: cicloId,
            IniciarNuevoCicloObraSocial: true,
            ConvenioCorroborado: true,
            MedicoId: 7,
            EsNuevoIngreso: true,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            MedicoUserId: medicoUserId);

        appointment.Reserve(patientId, notes: "Test notes", esTanda: true, tandaId: tandaId, operative: operative);

        Assert.Equal(AppointmentStatus.Ocupado, appointment.Status);
        Assert.Equal(patientId, appointment.PatientId);
        Assert.True(appointment.ReferidoTercero);
        Assert.Equal(42, appointment.ReferenteId);
        Assert.Equal("obra_social", appointment.ModalidadCobro);
        Assert.Equal(5, appointment.ObraSocialId);
        Assert.Equal("AUTH-12345", appointment.NumeroAutorizacion);
        Assert.Equal(10, appointment.SesionesAutorizadas);
        Assert.Equal(cicloId, appointment.CicloObraSocialId);
        Assert.True(appointment.IniciarNuevoCicloObraSocial);
        Assert.True(appointment.ConvenioCorroborado);
        Assert.Equal(7, appointment.MedicoId);
        Assert.Equal(medicoUserId, appointment.MedicoUserId);
        Assert.Null(appointment.MedicoNombre);
        Assert.True(appointment.EsNuevoIngreso);
        Assert.True(appointment.EsTanda);
        Assert.Equal(tandaId, appointment.TandaId);
    }

    [Fact]
    public void Hold_WithOperativeData_AppliesTandaAndMonoxidoFields()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var userId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();
        var apartadoTs = DateTimeOffset.UtcNow;

        var operative = new AppointmentOperativeData(
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "prepaga",
            ObraSocialId: 3,
            NumeroAutorizacion: "PREP-999",
            SesionesAutorizadas: 5,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: 2,
            EsNuevoIngreso: false,
            EsMonoxido: true,
            MonoxidoOrdenMedica: true,
            MonoxidoResumenClinico: true,
            MedicoUserId: null);

        appointment.Hold(patientId: null, userId: userId, apartadoTs: apartadoTs, notes: "Hold test", esMonoxido: true, tandaId: tandaId, operative: operative);

        Assert.Equal(AppointmentStatus.Apartado, appointment.Status);
        Assert.Equal(userId, appointment.ApartadoPorUserId);
        Assert.Equal(apartadoTs, appointment.ApartadoTs);
        Assert.True(appointment.EsMonoxido);
        Assert.True(appointment.MonoxidoOrdenMedica);
        Assert.True(appointment.MonoxidoResumenClinico);
        Assert.True(appointment.EsTanda);
        Assert.Equal(tandaId, appointment.TandaId);
        Assert.Equal("prepaga", appointment.ModalidadCobro);
        Assert.Equal(3, appointment.ObraSocialId);
        Assert.Equal("PREP-999", appointment.NumeroAutorizacion);
        Assert.Equal(2, appointment.MedicoId);
    }

    [Fact]
    public void AssignBlock_WithOperativeData_AppliesBlockAndBillingFields()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var blockId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();

        var operative = new AppointmentOperativeData(
            ReferidoTercero: true,
            ReferenteId: 99,
            ModalidadCobro: "convenio",
            ObraSocialId: 8,
            NumeroAutorizacion: "CONV-456",
            SesionesAutorizadas: 20,
            CicloObraSocialId: Guid.NewGuid(),
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: true,
            MedicoId: 15,
            EsNuevoIngreso: true,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            MedicoUserId: Guid.NewGuid());

        appointment.AssignBlock(blockId, esTanda: true, tandaId: tandaId, operative: operative);

        Assert.Equal(blockId, appointment.BlockId);
        Assert.True(appointment.EsBloqueCompleto);
        Assert.True(appointment.EsTanda);
        Assert.Equal(tandaId, appointment.TandaId);
        Assert.True(appointment.ReferidoTercero);
        Assert.Equal(99, appointment.ReferenteId);
        Assert.Equal("convenio", appointment.ModalidadCobro);
        Assert.Equal(8, appointment.ObraSocialId);
        Assert.Equal("CONV-456", appointment.NumeroAutorizacion);
        Assert.Equal(20, appointment.SesionesAutorizadas);
        Assert.True(appointment.ConvenioCorroborado);
        Assert.Equal(15, appointment.MedicoId);
        Assert.True(appointment.EsNuevoIngreso);
    }

    [Fact]
    public void UpdateOperativeData_WithBlankModalidad_UsesDefaultModalidad()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        appointment.Reserve(Guid.NewGuid(), operative: new AppointmentOperativeData(
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "obra_social",
            ObraSocialId: 1,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false));

        Assert.Equal("obra_social", appointment.ModalidadCobro);

        appointment.UpdateOperativeData(new AppointmentOperativeData(
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "   ",
            ObraSocialId: 2,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false));

        Assert.Equal(ModalidadCobroConstants.Default, appointment.ModalidadCobro);
        Assert.Equal(2, appointment.ObraSocialId);
    }

    [Fact]
    public void UpdateOperativeData_WithNullModalidad_UsesDefaultModalidad()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), TimeOnly.FromDateTime(DateTime.Now), 1);
        appointment.Reserve(Guid.NewGuid(), operative: new AppointmentOperativeData(
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "prepaga",
            ObraSocialId: 1,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false));

        appointment.UpdateOperativeData(new AppointmentOperativeData(
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: null,
            ObraSocialId: 3,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false));

        Assert.Equal(ModalidadCobroConstants.Default, appointment.ModalidadCobro);
        Assert.Equal(3, appointment.ObraSocialId);
    }

    [Fact]
    public void Release_ClearsOperativeDataAndReturnsLibre()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var tandaId = Guid.NewGuid();

        appointment.Reserve(Guid.NewGuid(), esTanda: true, tandaId: tandaId, operative: new AppointmentOperativeData(
            ReferidoTercero: true,
            ReferenteId: 10,
            ModalidadCobro: "obra_social",
            ObraSocialId: 5,
            NumeroAutorizacion: "AUTH-TEST",
            SesionesAutorizadas: 15,
            CicloObraSocialId: Guid.NewGuid(),
            IniciarNuevoCicloObraSocial: true,
            ConvenioCorroborado: true,
            MedicoId: 3,
            EsNuevoIngreso: true,
            EsMonoxido: true,
            MonoxidoOrdenMedica: true,
            MonoxidoResumenClinico: true,
            MedicoUserId: Guid.NewGuid()));

        Assert.Equal(AppointmentStatus.Ocupado, appointment.Status);
        Assert.True(appointment.ReferidoTercero);
        Assert.True(appointment.EsTanda);

        appointment.Release(notes: "Released for testing");

        Assert.Equal(AppointmentStatus.Libre, appointment.Status);
        Assert.False(appointment.ReferidoTercero);
        Assert.Null(appointment.ReferenteId);
        Assert.Equal(ModalidadCobroConstants.Default, appointment.ModalidadCobro);
        Assert.Null(appointment.ObraSocialId);
        Assert.Null(appointment.NumeroAutorizacion);
        Assert.Null(appointment.SesionesAutorizadas);
        Assert.Null(appointment.CicloObraSocialId);
        Assert.False(appointment.IniciarNuevoCicloObraSocial);
        Assert.False(appointment.ConvenioCorroborado);
        Assert.Null(appointment.MedicoId);
        Assert.Null(appointment.MedicoUserId);
        Assert.Null(appointment.MedicoNombre);
        Assert.False(appointment.EsNuevoIngreso);
        Assert.False(appointment.EsMonoxido);
        Assert.False(appointment.MonoxidoOrdenMedica);
        Assert.False(appointment.MonoxidoResumenClinico);
        Assert.False(appointment.EsTanda);
        Assert.Null(appointment.TandaId);
        Assert.Null(appointment.BlockId);
        Assert.False(appointment.EsBloqueCompleto);
        Assert.Equal("Released for testing", appointment.Notes);
    }

    [Fact]
    public void ConfirmHold_WithOperativeData_AppliesOperativeFields()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cicloId = Guid.NewGuid();

        appointment.Hold(patientId: null, userId: userId, apartadoTs: DateTimeOffset.UtcNow, esMonoxido: false);
        Assert.Equal(AppointmentStatus.Apartado, appointment.Status);

        var operative = new AppointmentOperativeData(
            ReferidoTercero: true,
            ReferenteId: 55,
            ModalidadCobro: "particular",
            ObraSocialId: null,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: cicloId,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: 8,
            EsNuevoIngreso: true,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            MedicoUserId: Guid.NewGuid());

        appointment.ConfirmHold(patientId: patientId, notes: "Confirmed with data", operative: operative);

        Assert.Equal(AppointmentStatus.Ocupado, appointment.Status);
        Assert.Equal(patientId, appointment.PatientId);
        Assert.True(appointment.ReferidoTercero);
        Assert.Equal(55, appointment.ReferenteId);
        Assert.Equal("particular", appointment.ModalidadCobro);
        Assert.Equal(cicloId, appointment.CicloObraSocialId);
        Assert.Equal(8, appointment.MedicoId);
        Assert.True(appointment.EsNuevoIngreso);
    }
}
