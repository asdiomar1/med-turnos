using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Schedules;

public interface ISchedulesService
{
    Task<IReadOnlyCollection<CameraSummary>> GetCamarasAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ScheduleHourSummary>> GetHorariosAsync(CancellationToken cancellationToken);
    Task<CameraSummary> CreateCamaraAsync(Guid actorUserId, string nombre, int capacidad, CancellationToken cancellationToken);
    Task<CameraMutationResult> UpdateCamaraAsync(Guid actorUserId, int id, string nombre, int capacidad, CancellationToken cancellationToken);
    Task<CameraSummary> UpdateCamaraEstadoAsync(Guid actorUserId, int id, bool activa, CancellationToken cancellationToken);
    Task<ScheduleHourSummary> CreateHorarioAsync(string hora, int orden, CancellationToken cancellationToken);
    Task<ScheduleHourSummary> UpdateHorarioAsync(int id, string hora, int orden, CancellationToken cancellationToken);
    Task<ScheduleHourSummary> UpdateHorarioEstadoAsync(int id, bool activo, CancellationToken cancellationToken);
    Task<int> GetHorarioDeletionPreviewAsync(int id, CancellationToken cancellationToken);
    Task<MutationResult> DeleteHorarioAsync(int id, CancellationToken cancellationToken);
}
