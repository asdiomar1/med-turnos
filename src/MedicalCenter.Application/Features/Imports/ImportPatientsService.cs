using System.Globalization;
using System.Text.Json;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;
using DomainClinicalHistory = MedicalCenter.Domain.Entities.ClinicalHistory;

namespace MedicalCenter.Application.Features.Imports;

public sealed class ImportPatientsService(
    IPatientRepository patientRepository,
    ICondicionIvaRepository condicionIvaRepository,
    IObraSocialRepository obraSocialRepository,
    IClinicalHistoryRepository clinicalHistoryRepository,
    IXlsxRowReader xlsxRowReader,
    IUnitOfWork unitOfWork) : IImportPatientsService
{
    private const string DocumentoIdentidadKey = "documento_identidad";
    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nombre"] = "nombre",
        ["email"] = "email",
        ["telefono"] = "telefono",
        [DocumentoIdentidadKey] = DocumentoIdentidadKey,
        ["documento"] = DocumentoIdentidadKey,
        ["dni"] = DocumentoIdentidadKey,
        ["documento_identidad_normalizado"] = "documento_identidad_normalizado",
        ["login_identifier"] = "login_identifier",
        ["nacionalidad"] = "nacionalidad",
        ["condicion_iva"] = "condicion_iva",
        ["condicion_iva_id"] = "condicion_iva_id",
        ["obra_social"] = "obra_social",
        ["obra_social_id"] = "obra_social_id",
        ["numero_credencial_obra_social"] = "numero_credencial_obra_social",
        ["portal_habilitado"] = "portal_habilitado",
        ["claustrofobico"] = "claustrofobico",
        ["notas"] = "notas",
        ["datos_extra"] = "datos_extra",
        ["opt_in_whatsapp"] = "opt_in_whatsapp",
        ["opt_in_source"] = "opt_in_source",
        ["numero_hc"] = "numero_hc"
    };

    public async Task<ImportPatientsResultDto> ImportAsync(Stream xlsxStream, CancellationToken cancellationToken)
    {
        var conditions = await condicionIvaRepository.GetAllAsync(includeInactive: true, cancellationToken);
        var obrasSociales = await obraSocialRepository.GetAllAsync(cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<ImportPatientRowErrorDto>();
        var total = 0;

        foreach (var rawRow in xlsxRowReader.Read(xlsxStream))
        {
            total++;
            try
            {
                var rowResult = await ProcessRowAsync(rawRow, conditions, obrasSociales, cancellationToken);
                if (rowResult == ImportRowResult.Created)
                {
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception exception)
            {
                skipped++;
                errors.Add(new ImportPatientRowErrorDto(total, exception.Message));
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportPatientsResultDto(total, created, updated, skipped, errors.Count, errors);
    }

    private async Task<ImportRowResult> ProcessRowAsync(
        IReadOnlyDictionary<string, string?> rawRow,
        IReadOnlyCollection<CondicionIva> conditions,
        IReadOnlyCollection<ObraSocial> obrasSociales,
        CancellationToken cancellationToken)
    {
        var row = MapRow(rawRow);
        ValidateRequiredFields(row);
        var context = BuildRowContext(row, conditions, obrasSociales);
        var existing = await patientRepository.GetByDocumentoAsync(row.DocumentoIdentidad!, cancellationToken);
        if (existing is null)
        {
            await CreatePatientAsync(row, context, cancellationToken);
            return ImportRowResult.Created;
        }

        await UpdatePatientAsync(existing, row, context, cancellationToken);
        return ImportRowResult.Updated;
    }

    private async Task CreatePatientAsync(PatientImportRow row, ImportPatientContext context, CancellationToken cancellationToken)
    {
        var patient = new Patient(
            Guid.NewGuid(),
            row.Nombre!.Trim(),
            new PatientAdministrativeInfo(
                row.Telefono!.Trim(),
                row.DocumentoIdentidad!.Trim(),
                context.NormalizedDoc,
                context.CondicionIvaId),
            new PatientPortalInfo(context.PortalHabilitado, context.LoginIdentifier));

        ApplyAdministrativeData(patient, row, context);
        ConfigurePortalAccess(patient, context.PortalHabilitado, context.LoginIdentifier);
        await patientRepository.AddAsync(patient, cancellationToken);
        await EnsureClinicalHistoryIfNeededAsync(patient.Id, row.NumeroHc, cancellationToken);
    }

    private async Task UpdatePatientAsync(Patient existing, PatientImportRow row, ImportPatientContext context, CancellationToken cancellationToken)
    {
        ApplyAdministrativeData(existing, row, context);
        ConfigurePortalAccess(existing, context.PortalHabilitado, context.LoginIdentifier);
        await EnsureClinicalHistoryIfNeededAsync(existing.Id, row.NumeroHc, cancellationToken);
    }

    private static void ValidateRequiredFields(PatientImportRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Nombre) || string.IsNullOrWhiteSpace(row.Telefono) || string.IsNullOrWhiteSpace(row.DocumentoIdentidad))
        {
            throw new ValidationException("nombre, telefono y documento_identidad son obligatorios.");
        }
    }

    private static ImportPatientContext BuildRowContext(
        PatientImportRow row,
        IReadOnlyCollection<CondicionIva> conditions,
        IReadOnlyCollection<ObraSocial> obrasSociales)
    {
        var normalizedDoc = NormalizeDocumento(row.DocumentoIdentidad!);
        var portalHabilitado = ParseBool(row.PortalHabilitado);
        var loginIdentifier = ResolveLoginIdentifier(row.LoginIdentifier, portalHabilitado, normalizedDoc);

        return new ImportPatientContext(
            normalizedDoc,
            ResolveCondicionIvaId(row, conditions),
            ResolveObraSocialId(row, obrasSociales),
            NormalizeJson(row.DatosExtra),
            portalHabilitado,
            ParseBool(row.OptInWhatsapp),
            loginIdentifier);
    }

    private static string? ResolveLoginIdentifier(string? loginIdentifier, bool portalHabilitado, string normalizedDoc)
    {
        var normalizedLogin = NormalizeOrNull(loginIdentifier);
        if (portalHabilitado && string.IsNullOrWhiteSpace(normalizedLogin))
        {
            return normalizedDoc;
        }

        return normalizedLogin;
    }

    private static void ApplyAdministrativeData(Patient patient, PatientImportRow row, ImportPatientContext context)
    {
        patient.UpdateAdministrativeData(new PatientAdministrativeDataUpdate(
            row.Email?.Trim(),
            row.Telefono!.Trim(),
            row.DocumentoIdentidad!.Trim(),
            context.NormalizedDoc,
            NormalizeOrNull(row.Nacionalidad),
            context.CondicionIvaId,
            context.ObraSocialId,
            NormalizeOrNull(row.NumeroCredencialObraSocial),
            ParseBool(row.Claustrofobico),
            NormalizeOrNull(row.Notas),
            context.DatosExtra,
            context.OptInWhatsapp,
            NormalizeOrNull(row.OptInSource)));
    }

    private static void ConfigurePortalAccess(Patient patient, bool portalHabilitado, string? loginIdentifier)
    {
        patient.ConfigurePortal(portalHabilitado, portalHabilitado);
        if (!string.IsNullOrWhiteSpace(loginIdentifier))
        {
            patient.SetLoginIdentifier(loginIdentifier);
        }
    }

    private async Task EnsureClinicalHistoryIfNeededAsync(Guid patientId, string? numeroHc, CancellationToken cancellationToken)
    {
        if (TryGetHistoryNumber(numeroHc, out var hcNumber))
        {
            await EnsureClinicalHistoryForPatientAsync(patientId, hcNumber, cancellationToken);
        }
    }

    private async Task EnsureClinicalHistoryForPatientAsync(Guid patientId, long number, CancellationToken cancellationToken)
    {
        var history = await clinicalHistoryRepository.GetByPatientIdAsync(patientId, cancellationToken);
        if (history is null)
        {
            await clinicalHistoryRepository.AddAsync(new DomainClinicalHistory(new ClinicalHistoryCreateParams(patientId, number, null, null, null, null)), cancellationToken);
        }
    }

    private static int ResolveCondicionIvaId(PatientImportRow row, IReadOnlyCollection<CondicionIva> conditions)
    {
        if (int.TryParse(NormalizeOrNull(row.CondicionIvaId), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
        {
            return id;
        }

        var name = NormalizeOrNull(row.CondicionIva);
        if (!string.IsNullOrWhiteSpace(name))
        {
            var found = conditions.FirstOrDefault(x => x.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return found.Id;
            }
        }

        throw new ValidationException("condicion_iva_id o condicion_iva son obligatorios.");
    }

    private static int? ResolveObraSocialId(PatientImportRow row, IReadOnlyCollection<ObraSocial> obrasSociales)
    {
        if (int.TryParse(NormalizeOrNull(row.ObraSocialId), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
        {
            return id;
        }

        var name = NormalizeOrNull(row.ObraSocial);
        if (!string.IsNullOrWhiteSpace(name))
        {
            var found = obrasSociales.FirstOrDefault(x => x.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase));
            return found?.Id;
        }

        return null;
    }

    private static bool TryGetHistoryNumber(string? value, out long number)
    {
        if (long.TryParse(NormalizeOrNull(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number > 0)
        {
            return true;
        }

        number = 0;
        return false;
    }

    private static string NormalizeDocumento(string documentoIdentidad) =>
        new string(documentoIdentidad.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string NormalizeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "{}";
        }

        using var doc = JsonDocument.Parse(value);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ValidationException("datos_extra invalido.");
        }

        return value;
    }

    private static string? NormalizeOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ParseBool(string? value)
    {
        var normalized = NormalizeOrNull(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return normalized.Equals("true", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("1", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("si", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("sí", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static PatientImportRow MapRow(IReadOnlyDictionary<string, string?> raw)
    {
        // Normalize all incoming header keys so aliases (dni → documento_identidad) resolve correctly
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in raw)
        {
            map[NormalizeHeader(k)] = v;
        }

        return new PatientImportRow(
            map.GetValueOrDefault("nombre"),
            map.GetValueOrDefault("email"),
            map.GetValueOrDefault("telefono"),
            map.GetValueOrDefault(DocumentoIdentidadKey),
            map.GetValueOrDefault("login_identifier"),
            map.GetValueOrDefault("nacionalidad"),
            map.GetValueOrDefault("condicion_iva_id"),
            map.GetValueOrDefault("condicion_iva"),
            map.GetValueOrDefault("obra_social_id"),
            map.GetValueOrDefault("obra_social"),
            map.GetValueOrDefault("numero_credencial_obra_social"),
            map.GetValueOrDefault("portal_habilitado"),
            map.GetValueOrDefault("claustrofobico"),
            map.GetValueOrDefault("notas"),
            map.GetValueOrDefault("datos_extra"),
            map.GetValueOrDefault("opt_in_whatsapp"),
            map.GetValueOrDefault("opt_in_source"),
            map.GetValueOrDefault("numero_hc"));
    }

    private static string NormalizeHeader(string header)
    {
        var normalized = header.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        return HeaderAliases.TryGetValue(normalized, out var mapped) ? mapped : normalized;
    }

    private sealed record PatientImportRow(
        string? Nombre,
        string? Email,
        string? Telefono,
        string? DocumentoIdentidad,
        string? LoginIdentifier,
        string? Nacionalidad,
        string? CondicionIvaId,
        string? CondicionIva,
        string? ObraSocialId,
        string? ObraSocial,
        string? NumeroCredencialObraSocial,
        string? PortalHabilitado,
        string? Claustrofobico,
        string? Notas,
        string? DatosExtra,
        string? OptInWhatsapp,
        string? OptInSource,
        string? NumeroHc);

    private sealed record ImportPatientContext(
        string NormalizedDoc,
        int CondicionIvaId,
        int? ObraSocialId,
        string DatosExtra,
        bool PortalHabilitado,
        bool OptInWhatsapp,
        string? LoginIdentifier);

    private enum ImportRowResult
    {
        Created,
        Updated
    }
}
