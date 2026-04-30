using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class PatientNoteRepository(MedicalCenterDbContext dbContext) : IPatientNoteRepository
{
    public async Task<IReadOnlyCollection<PatientNote>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken) =>
        await dbContext.PatientNotes
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PatientNote?> GetByIdAsync(Guid noteId, CancellationToken cancellationToken) =>
        dbContext.PatientNotes.FirstOrDefaultAsync(x => x.Id == noteId, cancellationToken);

    public Task AddAsync(PatientNote note, CancellationToken cancellationToken) =>
        dbContext.PatientNotes.AddAsync(note, cancellationToken).AsTask();

    public Task DeleteAsync(PatientNote note, CancellationToken cancellationToken)
    {
        dbContext.PatientNotes.Remove(note);
        return Task.CompletedTask;
    }
}
