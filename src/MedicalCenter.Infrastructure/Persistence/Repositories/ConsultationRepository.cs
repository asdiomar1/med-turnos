using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class ConsultationRepository(MedicalCenterDbContext dbContext) : IConsultationRepository
{
    public async Task<IReadOnlyCollection<ConsultationScheduleHour>> GetScheduleHoursAsync(CancellationToken cancellationToken) =>
        await dbContext.ConsultationScheduleHours.AsNoTracking().OrderBy(x => x.Orden).ToListAsync(cancellationToken);

    public Task<ConsultationScheduleHour?> GetScheduleHourByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.ConsultationScheduleHours.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> GetNextScheduleHourIdAsync(CancellationToken cancellationToken) =>
        (await dbContext.ConsultationScheduleHours.MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0) + 1;

    public Task AddScheduleHourAsync(ConsultationScheduleHour scheduleHour, CancellationToken cancellationToken) =>
        dbContext.ConsultationScheduleHours.AddAsync(scheduleHour, cancellationToken).AsTask();

    public Task AddAsync(ConsultationSlot slot, CancellationToken cancellationToken) =>
        dbContext.ConsultationSlots.AddAsync(slot, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<ConsultationSlot>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken) =>
        await dbContext.ConsultationSlots
            .Where(x => x.Fecha == fecha)
            .OrderBy(x => x.Hora)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ConsultationSlot>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken) =>
        await dbContext.ConsultationSlots
            .Where(x => x.Fecha >= fechaInicio && x.Fecha <= fechaFin)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ToListAsync(cancellationToken);

    public Task<ConsultationSlot?> GetByIdAsync(Guid slotId, CancellationToken cancellationToken) =>
        dbContext.ConsultationSlots.FirstOrDefaultAsync(x => x.Id == slotId, cancellationToken);

    public async Task<IReadOnlyCollection<ConsultationSession>> GetSessionsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ConsultationSessions
                .Where(x => x.PacienteId == patientId)
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return [];
        }
    }

    public async Task<int> CountFutureSlotsByHourAsync(TimeOnly hora, DateOnly fromDate, CancellationToken cancellationToken) =>
        await dbContext.ConsultationSlots.CountAsync(x => x.Hora == hora && x.Fecha >= fromDate, cancellationToken);
}
