using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Application.Features.WhatsApp;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments;

/// <summary>
/// Test fixture for AppointmentsService unit tests.
/// Provides 14 NSubstitute mocks and service factory method.
/// </summary>
public sealed class AppointmentsServiceTestFixture
{
    // Data Access Dependencies (10 mocks)
    public IAppointmentRepository AppointmentRepository { get; } = Substitute.For<IAppointmentRepository>();
    public IScheduleRepository ScheduleRepository { get; } = Substitute.For<IScheduleRepository>();
    public IUserRepository UserRepository { get; } = Substitute.For<IUserRepository>();
    public IScheduleHourRepository ScheduleHourRepository { get; } = Substitute.For<IScheduleHourRepository>();
    public ICameraRepository CameraRepository { get; } = Substitute.For<ICameraRepository>();
    public IPatientRepository PatientRepository { get; } = Substitute.For<IPatientRepository>();
    public IMedicoRepository MedicoRepository { get; } = Substitute.For<IMedicoRepository>();
    public IReferenteRepository ReferenteRepository { get; } = Substitute.For<IReferenteRepository>();
    public IObraSocialRepository ObraSocialRepository { get; } = Substitute.For<IObraSocialRepository>();
    public IBlockHistoryRepository BlockHistoryRepository { get; } = Substitute.For<IBlockHistoryRepository>();

    // Runtime Dependencies (4 mocks)
    public IWhatsappService WhatsappService { get; } = Substitute.For<IWhatsappService>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IIdempotencyStore IdempotencyStore { get; } = Substitute.For<IIdempotencyStore>();
    public IClock Clock { get; } = Substitute.For<IClock>();

    // Fixed test time for deterministic tests
    public DateTimeOffset FixedUtcNow { get; } = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    public TimeZoneInfo ArgentinaTimeZone { get; }

    public AppointmentsServiceTestFixture()
    {
        Clock.UtcNow.Returns(FixedUtcNow);
        ArgentinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
    }

    /// <summary>
    /// Creates the AppointmentsService under test with all mocked dependencies.
    /// </summary>
    public AppointmentsService CreateSut()
    {
        var dataAccess = new AppointmentsDataAccessDependencies(
            AppointmentRepository,
            ScheduleRepository,
            UserRepository,
            ScheduleHourRepository,
            CameraRepository,
            PatientRepository,
            MedicoRepository,
            ReferenteRepository,
            ObraSocialRepository,
            BlockHistoryRepository);

        var runtime = new AppointmentsRuntimeDependencies(
            WhatsappService,
            UnitOfWork,
            IdempotencyStore,
            Clock);

        return new AppointmentsService(dataAccess, runtime);
    }

    /// <summary>
    /// Resets all mock call counts and configurations between tests.
    /// Call this in the test class constructor or at the start of each test.
    /// </summary>
    public void Reset()
    {
        AppointmentRepository.ClearReceivedCalls();
        ScheduleRepository.ClearReceivedCalls();
        UserRepository.ClearReceivedCalls();
        ScheduleHourRepository.ClearReceivedCalls();
        CameraRepository.ClearReceivedCalls();
        PatientRepository.ClearReceivedCalls();
        MedicoRepository.ClearReceivedCalls();
        ReferenteRepository.ClearReceivedCalls();
        ObraSocialRepository.ClearReceivedCalls();
        BlockHistoryRepository.ClearReceivedCalls();
        WhatsappService.ClearReceivedCalls();
        UnitOfWork.ClearReceivedCalls();
        IdempotencyStore.ClearReceivedCalls();
        Clock.ClearReceivedCalls();

        // Restore fixed time after reset
        Clock.UtcNow.Returns(FixedUtcNow);
    }

    /// <summary>
    /// Gets the Argentina timezone DateOnly from the fixed UTC time.
    /// </summary>
    public DateOnly GetTodayInArgentina()
    {
        var argentinaTime = TimeZoneInfo.ConvertTimeFromUtc(FixedUtcNow.UtcDateTime, ArgentinaTimeZone);
        return DateOnly.FromDateTime(argentinaTime);
    }

    /// <summary>
    /// Gets a future date in Argentina timezone for testing.
    /// </summary>
    public DateOnly GetFutureDateInArgentina(int daysFromToday)
    {
        return GetTodayInArgentina().AddDays(daysFromToday);
    }
}
