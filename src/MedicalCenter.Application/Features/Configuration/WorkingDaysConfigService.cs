using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.Configuration;

public sealed class WorkingDaysConfigService(IDiasLaborablesConfigRepository repository) : IWorkingDaysConfigService
{
    private const string ConfigKey = "centro";
    private static readonly short[] DefaultDays = [1, 2, 3, 4, 5];

    public async Task<DiasLaborablesConfigDto> GetAsync(CancellationToken cancellationToken)
    {
        var config = await repository.GetAsync(ConfigKey, cancellationToken);
        return config is null ? new DiasLaborablesConfigDto(ConfigKey, DefaultDays) : Map(config);
    }

    public async Task<DiasLaborablesConfigDto> UpsertAsync(IReadOnlyCollection<short> diasSemana, CancellationToken cancellationToken)
    {
        var normalized = Normalize(diasSemana);
        var config = new DiasLaborablesConfig(ConfigKey, normalized);
        await repository.UpsertAsync(config, cancellationToken);
        return Map(config);
    }

    private static DiasLaborablesConfigDto Map(DiasLaborablesConfig config) => new(config.Id, config.DiasSemana);

    private static IReadOnlyCollection<short> Normalize(IReadOnlyCollection<short> diasSemana) =>
        (diasSemana.Count == 0 ? DefaultDays : diasSemana)
            .Distinct()
            .Where(x => x >= 0 && x <= 6)
            .OrderBy(x => x)
            .ToArray();
}
