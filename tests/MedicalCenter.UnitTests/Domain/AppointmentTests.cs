using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.UnitTests.Domain;

public sealed class AppointmentTests
{
    [Fact]
    public void Reserve_WhenAvailable_ChangesStatus()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);

        appointment.Reserve(Guid.NewGuid());

        Assert.Equal(AppointmentStatus.Ocupado, appointment.Status);
    }

    [Fact]
    public void Reserve_WhenNotAvailable_Throws()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        appointment.Reserve(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => appointment.Reserve(Guid.NewGuid()));
    }

    [Fact]
    public void Cancel_WhenOccupied_ChangesStatusToCancelado()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        appointment.Reserve(Guid.NewGuid());

        appointment.Cancel("Paciente no puede asistir");

        Assert.Equal(AppointmentStatus.Cancelado, appointment.Status);
        Assert.Null(appointment.PatientId);
    }

    [Fact]
    public void Cancel_WhenNotOccupied_Throws()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);

        Assert.Throws<InvalidOperationException>(() => appointment.Cancel());
    }

    [Fact]
    public void Hold_ThenConfirm_TransitionsToOccupied()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);
        var patientId = Guid.NewGuid();

        appointment.Hold(patientId, Guid.NewGuid(), DateTimeOffset.UtcNow);
        appointment.ConfirmHold();

        Assert.Equal(AppointmentStatus.Ocupado, appointment.Status);
        Assert.Equal(patientId, appointment.PatientId);
        Assert.Null(appointment.ApartadoPorUserId);
    }

    [Fact]
    public void ReleaseHold_WhenApartado_ReturnsToLibre()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 20), new TimeOnly(10, 0), 1);

        appointment.Hold(null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        appointment.ReleaseHold();

        Assert.Equal(AppointmentStatus.Libre, appointment.Status);
        Assert.Null(appointment.PatientId);
    }
}
