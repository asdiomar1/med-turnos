using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;
using DomainClinicalHistory = MedicalCenter.Domain.Entities.ClinicalHistory;

namespace MedicalCenter.Application.Features.ClinicalHistory;

public sealed class ClinicalHistoryService(
    IPatientRepository patientRepository,
    IMedicoRepository medicoRepository,
    IUserRepository userRepository,
    IClinicalHistoryRepository clinicalHistoryRepository,
    IUnitOfWork unitOfWork) : IClinicalHistoryService
{
    public Task<IReadOnlyCollection<ClinicalHistoryNumeroSummary>> GetResumenAsync(CancellationToken cancellationToken) =>
        clinicalHistoryRepository.GetResumenAsync(cancellationToken);

    public async Task<ClinicalHistorySummary> GetAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var history = await EnsureHistoryAsync(patientId, cancellationToken);
        return Map(history);
    }

    public async Task<ClinicalHistorySummary> UpdateAsync(Guid actorUserId, Guid patientId, string? antecedentes, string? alergias, string? medicacionActual, string? observacionesRelevantes, CancellationToken cancellationToken)
    {
        await EnsureCanManageAsync(actorUserId, "historia_clinica.editar_ficha", cancellationToken);
        var history = await EnsureHistoryAsync(patientId, cancellationToken);
        history.Update(antecedentes, alergias, medicacionActual, observacionesRelevantes);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(history);
    }

    public async Task<ClinicalHistorySummary> UpdateNumeroAsync(Guid actorUserId, Guid patientId, long numero, CancellationToken cancellationToken)
    {
        if (numero < 1)
        {
            throw new ValidationException("El número de historia clínica debe ser mayor a 0.");
        }

        await EnsureCanManageAsync(actorUserId, "historia_clinica.editar_numero", cancellationToken);
        var history = await EnsureHistoryAsync(patientId, cancellationToken);

        if (await clinicalHistoryRepository.IsNumeroTakenAsync(numero, patientId, cancellationToken))
        {
            throw new ConflictException($"El número {numero} ya está asignado a otra historia clínica.");
        }

        history.SetNumero(numero);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(history);
    }

    public async Task<IReadOnlyCollection<ClinicalEvolutionSummary>> GetEvolutionsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var evolutions = await clinicalHistoryRepository.GetEvolutionsByPatientIdAsync(patientId, cancellationToken);
        var medicos = await medicoRepository.GetAsync(false, cancellationToken);
        var medicoLookup = medicos.ToDictionary(x => x.Id, x => x);

        return evolutions.Select(x =>
        {
            medicoLookup.TryGetValue(x.MedicoId, out var medico);
            return Map(x, medico?.Nombre, medico?.Activo ?? false);
        }).ToArray();
    }

    public async Task<ClinicalEvolutionSummary> CreateEvolutionAsync(Guid actorUserId, Guid patientId, int medicoId, DateOnly fechaClinica, string? titulo, string nota, string? diagnosticoImpresion, string? indicaciones, Guid? consultaSlotId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nota))
        {
            throw new ValidationException("nota es obligatoria");
        }

        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
        if (!patient.IsActive)
        {
            throw new ConflictException("El paciente no se encuentra activo.");
        }

        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission("historia_clinica.crear_evolucion"))
        {
            throw new ForbiddenException("Prohibido");
        }

        var medico = await medicoRepository.GetByIdAsync(medicoId, cancellationToken) ?? throw new NotFoundException("Médico no encontrado");
        if (!medico.Activo)
        {
            throw new ConflictException("El médico no se encuentra activo.");
        }

        var evolution = new ClinicalEvolution(
            Guid.NewGuid(),
            patientId,
            consultaSlotId,
            medicoId,
            actor.Id,
            fechaClinica,
            titulo,
            nota.Trim(),
            diagnosticoImpresion,
            indicaciones);

        await clinicalHistoryRepository.AddEvolutionAsync(evolution, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(evolution, medico.Nombre, medico.Activo);
    }

    private async Task<DomainClinicalHistory> EnsureHistoryAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
        if (!patient.IsActive)
        {
            throw new ConflictException("El paciente no se encuentra activo.");
        }

        var history = await clinicalHistoryRepository.GetByPatientIdAsync(patientId, cancellationToken);
        if (history is not null)
        {
            return history;
        }

        var nextNumero = await clinicalHistoryRepository.GetNextNumeroAsync(cancellationToken);
        history = new DomainClinicalHistory(patientId, nextNumero, null, null, null, null);
        await clinicalHistoryRepository.AddAsync(history, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return history;
    }

    private async Task EnsureCanManageAsync(Guid actorUserId, string permission, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("Prohibido");
        }
    }

    private static ClinicalHistorySummary Map(DomainClinicalHistory history) =>
        new(history.PatientId, history.Numero, history.Antecedentes, history.Alergias, history.MedicacionActual, history.ObservacionesRelevantes, history.CreatedAt, history.UpdatedAt);

    private static ClinicalEvolutionSummary Map(ClinicalEvolution evolution, string? medicoNombre, bool medicoActivo) =>
        new(evolution.Id, evolution.PatientId, evolution.ConsultaSlotId, evolution.MedicoId, evolution.AuthorProfileId, evolution.FechaClinica, evolution.Titulo, evolution.Nota, evolution.DiagnosticoImpresion, evolution.Indicaciones, evolution.CreatedAt, evolution.UpdatedAt, medicoNombre, medicoActivo);
}
