using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class DiasLaborablesConfig : Entity<string>
{
    private static readonly short[] DefaultDias = [1, 2, 3, 4, 5];

    private DiasLaborablesConfig() { }

    public DiasLaborablesConfig(string key, IEnumerable<short>? diasSemana = null)
    {
        Id = key;
        DiasSemana = (diasSemana?.Distinct().OrderBy(x => x).ToArray() ?? DefaultDias).ToArray();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public short[] DiasSemana { get; private set; } = DefaultDias;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpsertDias(IEnumerable<short> diasSemana)
    {
        DiasSemana = diasSemana?.Distinct().OrderBy(x => x).ToArray() ?? DefaultDias;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
