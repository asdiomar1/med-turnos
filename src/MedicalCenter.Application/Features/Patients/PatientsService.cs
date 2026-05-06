using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;
using System.Text.Json;

namespace MedicalCenter.Application.Features.Patients;

public sealed class PatientsService(IPatientRepository patientRepository, IUserRepository userRepository, IAdminEventFeedRepository adminEventFeedRepository, IUnitOfWork unitOfWork) : IPatientsService
{
    private const string PatientNotFoundMessage = "Paciente no encontrado";
    public async Task<IReadOnlyCollection<PatientSummary>> GetAsync(string? search, bool includeInactive, CancellationToken cancellationToken)
    {
        var patients = await patientRepository.GetAsync(search, includeInactive, cancellationToken);
        return patients.Select(x => x.ToSummary()).ToArray();
    }

    public async Task<CreatedPatientResult> CreateAsync(Guid actorUserId, CreatePatientCommand command, CancellationToken cancellationToken)
    {
        Validate(new ValidationParams(command.Nombre, command.Telefono, command.DocumentoIdentidad, command.Nacionalidad, command.CondicionIvaId, command.ObraSocialId, command.NumeroCredencialObraSocial, command.DatosExtra));
        var normalizedLoginIdentifier = NormalizeOrNull(command.LoginIdentifier);
        if (!string.IsNullOrWhiteSpace(normalizedLoginIdentifier) &&
            await patientRepository.GetByLoginIdentifierAsync(normalizedLoginIdentifier, cancellationToken) is not null)
        {
            throw new ConflictException("login_identifier ya existe");
        }

        var patient = new Patient(
            Guid.NewGuid(),
            command.Nombre.Trim(),
            new PatientAdministrativeInfo(command.Telefono.Trim(), command.DocumentoIdentidad.Trim(), NormalizeDocumento(command.DocumentoIdentidad), command.CondicionIvaId),
            new PatientPortalInfo(command.PortalHabilitado, normalizedLoginIdentifier));
        patient.UpdateAdministrativeData(new PatientAdministrativeDataUpdate(
            command.Email?.Trim(),
            command.Telefono.Trim(),
            command.DocumentoIdentidad.Trim(),
            NormalizeDocumento(command.DocumentoIdentidad),
            command.Nacionalidad?.Trim(),
            command.CondicionIvaId,
            command.ObraSocialId,
            command.NumeroCredencialObraSocial?.Trim(),
            command.Claustrofobico,
            command.Notas?.Trim(),
            command.DatosExtra,
            command.OptInWhatsapp,
            command.OptInSource));
        patient.ConfigurePortal(command.PortalHabilitado, command.PortalHabilitado);

        await patientRepository.AddAsync(patient, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var entry = new AdminEventFeedEntry(new AdminEventFeedEntryCreateParams(
            0,
            DateTimeOffset.UtcNow,
            actorUserId,
            AdminEventFeedConstants.DefaultActorLabel,
            AdminEventFeedConstants.ActionCodes.PacienteCreated,
            AdminEventFeedConstants.ActionFamilyPatient,
            AdminEventFeedConstants.EntityTypes.Paciente,
            patient.Id.ToString(),
            null, null, null, null, null,
            "Paciente creado",
            $"Se creó el paciente \"{patient.Nombre}\".",
            AdminEventFeedConstants.SourceSystemApi,
            $"paciente:{AdminEventFeedConstants.ActionCodes.PacienteCreated}:{patient.Id}:{Guid.NewGuid():N}",
            "{}"));

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatedPatientResult(patient.Id, patient.Nombre);
    }

    public async Task<PatientSummary> UpdateAsync(Guid actorUserId, Guid patientId, UpdatePatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException(PatientNotFoundMessage);
        Validate(new ValidationParams(patient.Nombre, command.Telefono, command.DocumentoIdentidad, command.Nacionalidad, command.CondicionIvaId, command.ObraSocialId, command.NumeroCredencialObraSocial, command.DatosExtra));
        patient.UpdateAdministrativeData(new PatientAdministrativeDataUpdate(
            command.Email?.Trim(),
            command.Telefono.Trim(),
            command.DocumentoIdentidad.Trim(),
            NormalizeDocumento(command.DocumentoIdentidad),
            command.Nacionalidad?.Trim(),
            command.CondicionIvaId,
            command.ObraSocialId,
            command.NumeroCredencialObraSocial?.Trim(),
            command.Claustrofobico,
            command.ActualizarNotas ? command.Notas?.Trim() : patient.Notas,
            command.DatosExtra,
            command.OptInWhatsapp,
            command.OptInSource));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var entry = new AdminEventFeedEntry(new AdminEventFeedEntryCreateParams(
            0,
            DateTimeOffset.UtcNow,
            actorUserId,
            AdminEventFeedConstants.DefaultActorLabel,
            AdminEventFeedConstants.ActionCodes.PacienteUpdated,
            AdminEventFeedConstants.ActionFamilyPatient,
            AdminEventFeedConstants.EntityTypes.Paciente,
            patientId.ToString(),
            null, null, null, null, null,
            "Paciente actualizado",
            $"Se actualizaron los datos del paciente \"{patient.Nombre}\".",
            AdminEventFeedConstants.SourceSystemApi,
            $"paciente:{AdminEventFeedConstants.ActionCodes.PacienteUpdated}:{patientId}:{Guid.NewGuid():N}",
            "{}"));

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return patient.ToSummary();
    }

    public async Task<MutationResult> DeleteAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException(PatientNotFoundMessage);
        patient.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MutationResult(true);
    }

    public async Task<PatientSummary> ConfigurePortalAsync(Guid patientId, bool portalHabilitado, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException(PatientNotFoundMessage);
        if (portalHabilitado && string.IsNullOrWhiteSpace(patient.DocumentoIdentidadNormalizado))
        {
            throw new ValidationException("Habilitar portal exige documento valido");
        }

        patient.ConfigurePortal(portalHabilitado, portalHabilitado);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return patient.ToSummary();
    }

    public async Task<PatientSummary> EnableResetAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException(PatientNotFoundMessage);
        patient.MarkResetRequired();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return patient.ToSummary();
    }

    public async Task<PatientSummary> UpdateMyDataAsync(Guid userId, string nombre, string? email, string telefono, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
        if (user.PatientId is null)
        {
            throw new ForbiddenException();
        }

        var patient = await patientRepository.GetByIdAsync(user.PatientId.Value, cancellationToken) ?? throw new NotFoundException(PatientNotFoundMessage);
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(telefono))
        {
            throw new ValidationException("nombre y telefono son obligatorios");
        }

        patient.UpdateOwnData(nombre.Trim(), email?.Trim(), telefono.Trim());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return patient.ToSummary();
    }

    private sealed record ValidationParams(
        string Nombre,
        string Telefono,
        string DocumentoIdentidad,
        string? Nacionalidad,
        int CondicionIvaId,
        int? ObraSocialId,
        string? NumeroCredencialObraSocial,
        string DatosExtra);

    private static void Validate(ValidationParams p)
    {
        if (string.IsNullOrWhiteSpace(p.Nombre) || string.IsNullOrWhiteSpace(p.Telefono) || string.IsNullOrWhiteSpace(p.DocumentoIdentidad) || p.CondicionIvaId <= 0)
        {
            throw new ValidationException("telefono, documento y condicion_iva_id son obligatorios");
        }

        if (p.DocumentoIdentidad.Any(char.IsLetter) && string.IsNullOrWhiteSpace(p.Nacionalidad))
        {
            throw new ValidationException("nacionalidad es obligatoria cuando el documento contiene letras");
        }

        if (p.ObraSocialId.HasValue && string.IsNullOrWhiteSpace(p.NumeroCredencialObraSocial))
        {
            throw new ValidationException("numero_credencial_obra_social es obligatorio si hay obra social");
        }

        try
        {
            using var _ = JsonDocument.Parse(p.DatosExtra);
            if (_.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationException("datos_extra invalido");
            }
        }
        catch (JsonException)
        {
            throw new ValidationException("datos_extra invalido");
        }
    }

    private static string NormalizeDocumento(string documentoIdentidad) =>
        new string(documentoIdentidad.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string? NormalizeOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    }
