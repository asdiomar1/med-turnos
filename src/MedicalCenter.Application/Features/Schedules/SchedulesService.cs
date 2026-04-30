using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.Schedules;

public sealed class SchedulesService(
    ICameraRepository cameraRepository,
    IScheduleHourRepository scheduleHourRepository,
    IAppointmentRepository appointmentRepository,
    IScheduleRepository scheduleRepository,
    IAdminEventFeedRepository adminEventFeedRepository,
    IUnitOfWork unitOfWork) : ISchedulesService
{
    public async Task<IReadOnlyCollection<CameraSummary>> GetCamarasAsync(CancellationToken cancellationToken)
    {
        var cameras = await cameraRepository.GetAsync(cancellationToken);
        return cameras.Select(x => new CameraSummary(x.Id, x.Nombre, x.Capacidad, x.Activa)).ToArray();
    }

    public async Task<IReadOnlyCollection<ScheduleHourSummary>> GetHorariosAsync(CancellationToken cancellationToken)
    {
        var hours = await scheduleHourRepository.GetAsync(cancellationToken);
        return hours.Select(x => new ScheduleHourSummary(x.Id, x.Hora, x.Orden, x.Activo)).ToArray();
    }

    public async Task<CameraSummary> CreateCamaraAsync(Guid actorUserId, string nombre, int capacidad, CancellationToken cancellationToken)
    {
        ValidateCamera(nombre, capacidad);
        var camera = new MedicalCenter.Domain.Entities.Camera(await cameraRepository.GetNextIdAsync(cancellationToken), nombre.Trim(), capacidad, true);
        await cameraRepository.AddAsync(camera, cancellationToken);
        await RegisterCameraEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CameraCreated,
            AdminEventFeedConstants.ActionFamilyCatalog,
            camera.Id.ToString(),
            "Cámara creada",
            $"Se creó la cámara \"{camera.Nombre}\" con capacidad {camera.Capacidad}.",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CameraSummary(camera.Id, camera.Nombre, camera.Capacidad, camera.Activa);
    }

    public async Task<CameraMutationResult> UpdateCamaraAsync(Guid actorUserId, int id, string nombre, int capacidad, CancellationToken cancellationToken)
    {
        ValidateCamera(nombre, capacidad);
        var camera = await cameraRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Camara no encontrada");
        var previousNombre = camera.Nombre;
        var previousCapacidad = camera.Capacidad;
        camera.Update(nombre.Trim(), capacidad);

        var cancelados = 0;
        var apartadosLiberados = 0;
        var eliminados = 0;

        if (capacidad < previousCapacidad)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var excess = await appointmentRepository.GetFutureExcessByCameraAsync(id, today, capacidad, cancellationToken);

            foreach (var slot in excess)
            {
                if (slot.Status == AppointmentStatus.Ocupado)
                {
                    slot.Cancel("Reducción de capacidad de cámara");
                    cancelados++;
                }
                else if (slot.Status == AppointmentStatus.Apartado)
                {
                    slot.ReleaseHold("Reducción de capacidad de cámara");
                    apartadosLiberados++;
                }
            }

            var scheduleIds = excess.Select(x => x.ScheduleId).ToList();
            await appointmentRepository.DeleteRangeAsync(excess, cancellationToken);
            await scheduleRepository.DeleteRangeByIdsAsync(scheduleIds, cancellationToken);
            eliminados = excess.Count;
        }

        await RegisterCameraEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CameraUpdated,
            AdminEventFeedConstants.ActionFamilyCatalog,
            camera.Id.ToString(),
            "Cámara actualizada",
            $"Se actualizó la cámara \"{previousNombre}\" → \"{camera.Nombre}\" (capacidad {previousCapacidad} → {camera.Capacidad}). Slots eliminados: {eliminados}, cancelados: {cancelados}, apartados liberados: {apartadosLiberados}.",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CameraMutationResult(new CameraSummary(camera.Id, camera.Nombre, camera.Capacidad, camera.Activa), 0, cancelados, apartadosLiberados, eliminados);
    }

    public async Task<CameraSummary> UpdateCamaraEstadoAsync(Guid actorUserId, int id, bool activa, CancellationToken cancellationToken)
    {
        var camera = await cameraRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Camara no encontrada");
        camera.SetActiva(activa);
        await RegisterCameraEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CameraStatusUpdated,
            AdminEventFeedConstants.ActionFamilyCatalog,
            camera.Id.ToString(),
            "Estado de cámara actualizado",
            $"La cámara \"{camera.Nombre}\" quedó {(camera.Activa ? "activa" : "inactiva")}.",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CameraSummary(camera.Id, camera.Nombre, camera.Capacidad, camera.Activa);
    }

    public async Task<ScheduleHourSummary> CreateHorarioAsync(string hora, int orden, CancellationToken cancellationToken)
    {
        ValidateHour(hora, orden);
        var hour = new MedicalCenter.Domain.Entities.ScheduleHour(await scheduleHourRepository.GetNextIdAsync(cancellationToken), hora.Trim(), orden, true);
        await scheduleHourRepository.AddAsync(hour, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ScheduleHourSummary(hour.Id, hour.Hora, hour.Orden, hour.Activo);
    }

    public async Task<ScheduleHourSummary> UpdateHorarioAsync(int id, string hora, int orden, CancellationToken cancellationToken)
    {
        ValidateHour(hora, orden);
        var scheduleHour = await scheduleHourRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Horario no encontrado");
        var futureSlots = await appointmentRepository.GetByRangeAsync(DateOnly.FromDateTime(DateTime.UtcNow.Date), DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(2)), cancellationToken);
        if (!string.Equals(scheduleHour.Hora, hora.Trim(), StringComparison.Ordinal) && futureSlots.Any(x => x.Hora == TimeOnly.Parse(scheduleHour.Hora)))
        {
            throw new ConflictException("No se puede cambiar la hora de un horario con slots futuros existentes");
        }

        scheduleHour.Update(hora.Trim(), orden);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ScheduleHourSummary(scheduleHour.Id, scheduleHour.Hora, scheduleHour.Orden, scheduleHour.Activo);
    }

    public async Task<ScheduleHourSummary> UpdateHorarioEstadoAsync(int id, bool activo, CancellationToken cancellationToken)
    {
        var scheduleHour = await scheduleHourRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Horario no encontrado");
        scheduleHour.SetActivo(activo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ScheduleHourSummary(scheduleHour.Id, scheduleHour.Hora, scheduleHour.Orden, scheduleHour.Activo);
    }

    public async Task<int> GetHorarioDeletionPreviewAsync(int id, CancellationToken cancellationToken)
    {
        var scheduleHour = await scheduleHourRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Horario no encontrado");
        var futureSlots = await appointmentRepository.GetByRangeAsync(DateOnly.FromDateTime(DateTime.UtcNow.Date), DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(2)), cancellationToken);
        return futureSlots.Count(x => x.Hora == TimeOnly.Parse(scheduleHour.Hora));
    }

    public async Task<MutationResult> DeleteHorarioAsync(int id, CancellationToken cancellationToken)
    {
        var scheduleHour = await scheduleHourRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Horario no encontrado");
        scheduleHour.SetActivo(false);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MutationResult(true);
    }

    private static void ValidateCamera(string nombre, int capacidad)
    {
        if (string.IsNullOrWhiteSpace(nombre) || capacidad < 1)
        {
            throw new ValidationException("Camara requiere nombre y capacidad >= 1");
        }
    }

    private static void ValidateHour(string hora, int orden)
    {
        if (!TimeOnly.TryParse(hora, out _) || hora.Length != 5)
        {
            throw new ValidationException("Hora invalida");
        }

        if (orden < 1)
        {
            throw new ValidationException("Orden invalido");
        }
    }

    private async Task RegisterCameraEventAsync(
        Guid actorUserId,
        string actionCode,
        string actionFamily,
        string entityId,
        string title,
        string summary,
        CancellationToken cancellationToken)
    {
        var entry = new MedicalCenter.Domain.Entities.AdminEventFeedEntry(
            0,
            DateTimeOffset.UtcNow,
            actorUserId,
            AdminEventFeedConstants.DefaultActorLabel,
            actionCode,
            actionFamily,
            AdminEventFeedConstants.EntityTypes.Camera,
            entityId,
            "turnos",
            null,
            null,
            null,
            null,
            title,
            summary,
            AdminEventFeedConstants.SourceSystemApi,
            $"camera:{actionCode}:{entityId}:{Guid.NewGuid():N}",
            "{}");

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
    }
}
