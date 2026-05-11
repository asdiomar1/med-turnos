using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;
using NSubstitute;
using Xunit;

namespace MedicalCenter.UnitTests.Features.Appointments;

public sealed class AppointmentsServiceTandaTests : IClassFixture<AppointmentsServiceTestFixture>
{
    private readonly AppointmentsServiceTestFixture _fixture;

    public AppointmentsServiceTandaTests(AppointmentsServiceTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    [Fact]
    public async Task RescheduleAsync_TandaScope_PreservesPatientMapping()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();
        var patient1Id = Guid.NewGuid();
        var patient2Id = Guid.NewGuid();
        var sourceSlot1Id = Guid.NewGuid();
        var sourceSlot2Id = Guid.NewGuid();
        var targetSlot1Id = Guid.NewGuid();
        var targetSlot2Id = Guid.NewGuid();
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);
        var targetDate = futureDate.AddDays(1);

        var sourceSlot1 = new AppointmentBuilder()
            .WithId(sourceSlot1Id)
            .WithFecha(futureDate)
            .WithHora(10, 0)
            .WithLugar(0)
            .WithTanda(tandaId)
            .AsOcupado(patient1Id)
            .Build();

        var sourceSlot2 = new AppointmentBuilder()
            .WithId(sourceSlot2Id)
            .WithFecha(futureDate)
            .WithHora(10, 0)
            .WithLugar(1)
            .WithTanda(tandaId)
            .AsOcupado(patient2Id)
            .Build();

        var targetSlot1 = new AppointmentBuilder()
            .WithId(targetSlot1Id)
            .WithFecha(targetDate)
            .WithHora(11, 0)
            .WithLugar(0)
            .AsLibre()
            .Build();

        var targetSlot2 = new AppointmentBuilder()
            .WithId(targetSlot2Id)
            .WithFecha(targetDate)
            .WithHora(11, 0)
            .WithLugar(1)
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlot1Id, Arg.Any<CancellationToken>()).Returns(sourceSlot1);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlot1Id, Arg.Any<CancellationToken>()).Returns(targetSlot1);
        _fixture.AppointmentRepository.GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>()).Returns(new[] { sourceSlot1, sourceSlot2 });
        _fixture.AppointmentRepository.GetByBlockAsync(targetDate, new TimeOnly(11, 0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { targetSlot1, targetSlot2 });
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(Arg.Any<string>(), Arg.Any<string>());

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlot1Id, "tanda");

        // Act
        await sut.RescheduleAsync(actorUserId, sourceSlot1Id, "idem-tanda", command, CancellationToken.None);

        // Assert
        Assert.Equal(patient1Id, targetSlot1.PatientId);
        Assert.Equal(patient2Id, targetSlot2.PatientId);
        Assert.Equal(AppointmentStatus.Reprogramado, sourceSlot1.Status);
        Assert.Equal(AppointmentStatus.Reprogramado, sourceSlot2.Status);
    }
}
