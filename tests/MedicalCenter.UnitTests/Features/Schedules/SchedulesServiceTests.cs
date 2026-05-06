using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Schedules;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Schedules;

public sealed class SchedulesServiceTests
{
    [Fact]
    public async Task UpdateHorarioAsync_WhenExistingHourHasFutureSlots_ThrowsConflict()
    {
        var scheduleHourRepository = Substitute.For<IScheduleHourRepository>();
        var appointmentRepository = Substitute.For<IAppointmentRepository>();
        var service = CreateService(scheduleHourRepository, appointmentRepository);

        scheduleHourRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new ScheduleHour(1, "09:30", 1, true));

        appointmentRepository
            .GetByRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), null, null, Arg.Any<CancellationToken>())
            .Returns([
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 30), 1, 1)
            ]);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.UpdateHorarioAsync(1, "10:00", 1, CancellationToken.None));
    }

    [Fact]
    public async Task GetHorarioDeletionPreviewAsync_CountsSlotsWithInvariantHour()
    {
        var scheduleHourRepository = Substitute.For<IScheduleHourRepository>();
        var appointmentRepository = Substitute.For<IAppointmentRepository>();
        var service = CreateService(scheduleHourRepository, appointmentRepository);

        scheduleHourRepository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(new ScheduleHour(2, "09:30", 1, true));

        appointmentRepository
            .GetByRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), null, null, Arg.Any<CancellationToken>())
            .Returns([
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 30), 1, 1),
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 30), 2, 1),
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(10, 0), 3, 1)
            ]);

        var result = await service.GetHorarioDeletionPreviewAsync(2, CancellationToken.None);

        Assert.Equal(2, result);
    }

    private static SchedulesService CreateService(
        IScheduleHourRepository scheduleHourRepository,
        IAppointmentRepository appointmentRepository)
    {
        var cameraRepository = Substitute.For<ICameraRepository>();
        var scheduleRepository = Substitute.For<IScheduleRepository>();
        var adminEventFeedRepository = Substitute.For<IAdminEventFeedRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        return new SchedulesService(
            cameraRepository,
            scheduleHourRepository,
            appointmentRepository,
            scheduleRepository,
            adminEventFeedRepository,
            unitOfWork);
    }
}
