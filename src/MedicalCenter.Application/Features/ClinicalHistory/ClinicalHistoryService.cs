using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Mappings;
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
        return history.ToSummary();
    }

    public async Task<ClinicalHistorySummary> UpdateAsync(Guid actorUserId, Guid patientId, string? antecedentes, string? alergias, string? medicacionActual, string? observacionesRelevantes, CancellationToken cancellationToken)
    {
        await EnsureCanManageAsync(actorUserId, "historia_clinica.editar_ficha", cancellationToken);
        var history = await EnsureHistoryAsync(patientId, cancellationToken);
        history.Update(antecedentes, alergias, medicacionActual, observacionesRelevantes);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return history.ToSummary();
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
        return history.ToSummary();
    }

    public async Task<IReadOnlyCollection<ClinicalEvolutionSummary>> GetEvolutionsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var evolutions = await clinicalHistoryRepository.GetEvolutionsByPatientIdAsync(patientId, cancellationToken);

        var legacyMedicoIds = evolutions.Where(x => x.MedicoId > 0 && !x.MedicoUserId.HasValue).Select(x => x.MedicoId).ToHashSet();
        var medicoLookup = legacyMedicoIds.Count > 0
            ? (await medicoRepository.GetAsync(false, cancellationToken)).Where(x => legacyMedicoIds.Contains(x.Id)).ToDictionary(x => x.Id)
            : new Dictionary<int, Medico>();

        var result = new List<ClinicalEvolutionSummary>();
        foreach (var x in evolutions)
        {
            if (x.MedicoUserId.HasValue)
            {
                var medicoUser = await userRepository.GetByIdAsync(x.MedicoUserId.Value, cancellationToken);
                result.Add(x.ToSummary(medicoUser?.Nombre ?? medicoUser?.Identifier, medicoUser?.IsActive ?? false));
            }
            else
            {
                medicoLookup.TryGetValue(x.MedicoId, out var medico);
                result.Add(x.ToSummary(medico?.Nombre, medico?.Activo ?? false));
            }
        }

        return result;
    }

    public async Task<ClinicalEvolutionSummary> CreateEvolutionAsync(Guid actorUserId, Guid patientId, int? medicoId, DateOnly fechaClinica, string? titulo, string nota, string? diagnosticoImpresion, string? indicaciones, Guid? consultaSlotId, CancellationToken cancellationToken, Guid? medicoUserId = null)
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

        string? medicoNombre;
        bool medicoActivo;

        if (medicoUserId.HasValue)
        {
            var medicoUser = await userRepository.GetByIdAsync(medicoUserId.Value, cancellationToken) ?? throw new NotFoundException("Médico no encontrado");
            if (!medicoUser.IsActive)
                throw new ConflictException("El médico no se encuentra activo.");
            if (!medicoUser.Roles.Any(r => r.Code == "medico"))
                throw new ConflictException("El usuario no tiene rol de médico.");
            medicoNombre = medicoUser.Nombre ?? medicoUser.Identifier;
            medicoActivo = medicoUser.IsActive;
        }
        else if (medicoId.HasValue)
        {
            var medico = await medicoRepository.GetByIdAsync(medicoId.Value, cancellationToken) ?? throw new NotFoundException("Médico no encontrado");
            if (!medico.Activo)
                throw new ConflictException("El médico no se encuentra activo.");
            medicoNombre = medico.Nombre;
            medicoActivo = medico.Activo;
        }
        else
        {
            throw new ValidationException("Médico requerido.");
        }

        var evolution = new ClinicalEvolution(
            Guid.NewGuid(),
            patientId,
            consultaSlotId,
            medicoId ?? 0,
            actor.Id,
            fechaClinica,
            titulo,
            nota.Trim(),
            diagnosticoImpresion,
            indicaciones,
            medicoUserId: medicoUserId);

        await clinicalHistoryRepository.AddEvolutionAsync(evolution, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return evolution.ToSummary(medicoNombre, medicoActivo);
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

    }
