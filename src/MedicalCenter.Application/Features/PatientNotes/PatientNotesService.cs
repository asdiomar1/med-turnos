using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.PatientNotes;

public sealed class PatientNotesService(
    IPatientRepository patientRepository,
    IPatientNoteRepository patientNoteRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IPatientNotesService
{
    public async Task<IReadOnlyCollection<PatientNoteSummary>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var notes = await patientNoteRepository.GetByPatientIdAsync(patientId, cancellationToken);
        return notes.Select(n => n.ToSummary()).ToArray();
    }

    public async Task<PatientNoteSummary> CreateAsync(Guid actorUserId, Guid patientId, string mensaje, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            throw new ValidationException("mensaje es obligatorio");
        }

        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
        if (!patient.IsActive)
        {
            throw new ConflictException("El paciente no se encuentra activo.");
        }

        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission("historia_clinica.editar_ficha"))
        {
            throw new ForbiddenException("Prohibido");
        }

        var note = new PatientNote(Guid.NewGuid(), patient.Id, actorUserId, mensaje.Trim(), DateTimeOffset.UtcNow);
        await patientNoteRepository.AddAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return note.ToSummary();
    }

    public async Task DeleteAsync(Guid actorUserId, Guid noteId, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission("historia_clinica.editar_ficha"))
        {
            throw new ForbiddenException("Prohibido");
        }

        var note = await patientNoteRepository.GetByIdAsync(noteId, cancellationToken) ?? throw new NotFoundException("Nota no encontrada");
        await patientNoteRepository.DeleteAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
