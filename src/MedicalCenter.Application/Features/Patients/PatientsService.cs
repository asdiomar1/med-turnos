using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;
using System.Text.Json;

namespace MedicalCenter.Application.Features.Patients;

public sealed class PatientsService(IPatientRepository patientRepository, IUserRepository userRepository, IUnitOfWork unitOfWork) : IPatientsService
{
    public async Task<IReadOnlyCollection<PatientSummary>> GetAsync(string? search, bool includeInactive, CancellationToken cancellationToken)
    {
        var patients = await patientRepository.GetAsync(search, includeInactive, cancellationToken);
        return patients.Select(x => x.ToSummary()).ToArray();
    }

    public async Task<CreatedPatientResult> CreateAsync(
        string nombre,
        string? email,
        string telefono,
        string documentoIdentidad,
        string? loginIdentifier,
        string? nacionalidad,
        int condicionIvaId,
        int? obraSocialId,
        string? numeroCredencialObraSocial,
        bool portalHabilitado,
        bool optInWhatsapp,
        string? optInSource,
        bool claustrofobico,
        string? notas,
        string datosExtra,
        CancellationToken cancellationToken)
    {
        Validate(nombre, telefono, documentoIdentidad, nacionalidad, condicionIvaId, obraSocialId, numeroCredencialObraSocial, datosExtra);
        var normalizedLoginIdentifier = NormalizeOrNull(loginIdentifier);
        if (!string.IsNullOrWhiteSpace(normalizedLoginIdentifier) &&
            await patientRepository.GetByLoginIdentifierAsync(normalizedLoginIdentifier, cancellationToken) is not null)
        {
            throw new ConflictException("login_identifier ya existe");
        }

        var patient = new Patient(
            Guid.NewGuid(),
            nombre.Trim(),
            telefono.Trim(),
            documentoIdentidad.Trim(),
            NormalizeDocumento(documentoIdentidad),
            condicionIvaId,
            portalHabilitado,
            normalizedLoginIdentifier);
        patient.UpdateAdministrativeData(email?.Trim(), telefono.Trim(), documentoIdentidad.Trim(), NormalizeDocumento(documentoIdentidad), nacionalidad?.Trim(), condicionIvaId, obraSocialId, numeroCredencialObraSocial?.Trim(), claustrofobico, notas?.Trim(), datosExtra, optInWhatsapp, optInSource);
        patient.ConfigurePortal(portalHabilitado, portalHabilitado);

        await patientRepository.AddAsync(patient, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreatedPatientResult(patient.Id, patient.Nombre);
    }

    public async Task<PatientSummary> UpdateAsync(Guid patientId, string? email, string telefono, string documentoIdentidad, string? nacionalidad, int condicionIvaId, int? obraSocialId, string? numeroCredencialObraSocial, bool claustrofobico, string? notas, string datosExtra, bool actualizarNotas, bool optInWhatsapp, string? optInSource, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
        Validate(patient.Nombre, telefono, documentoIdentidad, nacionalidad, condicionIvaId, obraSocialId, numeroCredencialObraSocial, datosExtra);
        patient.UpdateAdministrativeData(email?.Trim(), telefono.Trim(), documentoIdentidad.Trim(), NormalizeDocumento(documentoIdentidad), nacionalidad?.Trim(), condicionIvaId, obraSocialId, numeroCredencialObraSocial?.Trim(), claustrofobico, actualizarNotas ? notas?.Trim() : patient.Notas, datosExtra, optInWhatsapp, optInSource);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return patient.ToSummary();
    }

    public async Task<MutationResult> DeleteAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
        patient.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MutationResult(true);
    }

    public async Task<PatientSummary> ConfigurePortalAsync(Guid patientId, bool portalHabilitado, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
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
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
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

        var patient = await patientRepository.GetByIdAsync(user.PatientId.Value, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado");
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(telefono))
        {
            throw new ValidationException("nombre y telefono son obligatorios");
        }

        patient.UpdateOwnData(nombre.Trim(), email?.Trim(), telefono.Trim());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return patient.ToSummary();
    }

    private static void Validate(string nombre, string telefono, string documentoIdentidad, string? nacionalidad, int condicionIvaId, int? obraSocialId, string? numeroCredencialObraSocial, string datosExtra)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(documentoIdentidad) || condicionIvaId <= 0)
        {
            throw new ValidationException("telefono, documento y condicion_iva_id son obligatorios");
        }

        if (documentoIdentidad.Any(char.IsLetter) && string.IsNullOrWhiteSpace(nacionalidad))
        {
            throw new ValidationException("nacionalidad es obligatoria cuando el documento contiene letras");
        }

        if (obraSocialId.HasValue && string.IsNullOrWhiteSpace(numeroCredencialObraSocial))
        {
            throw new ValidationException("numero_credencial_obra_social es obligatorio si hay obra social");
        }

        try
        {
            using var _ = JsonDocument.Parse(datosExtra);
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
