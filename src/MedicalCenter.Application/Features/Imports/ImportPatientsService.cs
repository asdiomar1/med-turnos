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
    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nombre"] = "nombre",
        ["email"] = "email",
        ["telefono"] = "telefono",
        ["documento_identidad"] = "documento_identidad",
        ["documento"] = "documento_identidad",
        ["dni"] = "documento_identidad",
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
                var row = MapRow(rawRow);
                if (string.IsNullOrWhiteSpace(row.Nombre) || string.IsNullOrWhiteSpace(row.Telefono) || string.IsNullOrWhiteSpace(row.DocumentoIdentidad))
                {
                    throw new ValidationException("nombre, telefono y documento_identidad son obligatorios.");
                }

                var normalizedDoc = NormalizeDocumento(row.DocumentoIdentidad);
                var condicionIvaId = ResolveCondicionIvaId(row, conditions);
                var obraSocialId = ResolveObraSocialId(row, obrasSociales);
                var datosExtra = NormalizeJson(row.DatosExtra);
                var portalHabilitado = ParseBool(row.PortalHabilitado);
                var optInWhatsapp = ParseBool(row.OptInWhatsapp);
                var loginIdentifier = NormalizeOrNull(row.LoginIdentifier);
                if (portalHabilitado && string.IsNullOrWhiteSpace(loginIdentifier))
                {
                    loginIdentifier = normalizedDoc;
                }

                var existing = await patientRepository.GetByDocumentoAsync(row.DocumentoIdentidad, cancellationToken);
                if (existing is null)
                {
                    var patient = new Patient(
                        Guid.NewGuid(),
                        row.Nombre.Trim(),
                        row.Telefono.Trim(),
                        row.DocumentoIdentidad.Trim(),
                        normalizedDoc,
                        condicionIvaId,
                        portalHabilitado,
                        loginIdentifier);

                    patient.UpdateAdministrativeData(
                        row.Email?.Trim(),
                        row.Telefono.Trim(),
                        row.DocumentoIdentidad.Trim(),
                        normalizedDoc,
                        NormalizeOrNull(row.Nacionalidad),
                        condicionIvaId,
                        obraSocialId,
                        NormalizeOrNull(row.NumeroCredencialObraSocial),
                        ParseBool(row.Claustrofobico),
                        NormalizeOrNull(row.Notas),
                        datosExtra,
                        optInWhatsapp,
                        NormalizeOrNull(row.OptInSource));

                    patient.ConfigurePortal(portalHabilitado, portalHabilitado);
                    if (!string.IsNullOrWhiteSpace(loginIdentifier))
                    {
                        patient.SetLoginIdentifier(loginIdentifier);
                    }

                    await patientRepository.AddAsync(patient, cancellationToken);

                    if (TryGetHistoryNumber(row.NumeroHc, out var hcNumber))
                    {
                        await EnsureClinicalHistoryForPatientAsync(patient.Id, hcNumber, cancellationToken);
                    }

                    created++;
                }
                else
                {
                    existing.UpdateAdministrativeData(
                        row.Email?.Trim(),
                        row.Telefono.Trim(),
                        row.DocumentoIdentidad.Trim(),
                        normalizedDoc,
                        NormalizeOrNull(row.Nacionalidad),
                        condicionIvaId,
                        obraSocialId,
                        NormalizeOrNull(row.NumeroCredencialObraSocial),
                        ParseBool(row.Claustrofobico),
                        NormalizeOrNull(row.Notas),
                        datosExtra,
                        optInWhatsapp,
                        NormalizeOrNull(row.OptInSource));

                    if (!string.IsNullOrWhiteSpace(loginIdentifier))
                    {
                        existing.SetLoginIdentifier(loginIdentifier);
                    }

                    existing.ConfigurePortal(portalHabilitado, portalHabilitado);

                    if (TryGetHistoryNumber(row.NumeroHc, out var hcNumber))
                    {
                        await EnsureClinicalHistoryForPatientAsync(existing.Id, hcNumber, cancellationToken);
                    }

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

    private async Task EnsureClinicalHistoryForPatientAsync(Guid patientId, long number, CancellationToken cancellationToken)
    {
        var history = await clinicalHistoryRepository.GetByPatientIdAsync(patientId, cancellationToken);
        if (history is null)
        {
            await clinicalHistoryRepository.AddAsync(new DomainClinicalHistory(patientId, number, null, null, null, null), cancellationToken);
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
            map.GetValueOrDefault("documento_identidad"),
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
}
