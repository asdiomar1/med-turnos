using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.AdminEventFeed;

public sealed class AdminEventFeedService(IAdminEventFeedRepository repository) : IAdminEventFeedService
{
    public async Task<IReadOnlyCollection<AdminEventFeedItemDto>> ListAsync(AdminEventFeedQuery query, CancellationToken cancellationToken)
    {
        var normalizedActionCodes = query.ActionCodes
            .Select(NormalizeText)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var items = await repository.ListAsync(query with { ActionCodes = normalizedActionCodes }, cancellationToken);

        return items.Select(Map).ToArray();
    }

    public Task<AdminEventFeedFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken) =>
        repository.GetFilterOptionsAsync(cancellationToken);

    private static AdminEventFeedItemDto Map(AdminEventFeedEntry entry) =>
        new(
            entry.Id,
            entry.OccurredAt,
            entry.ActorUserId,
            entry.ActorLabel,
            entry.ActionCode,
            entry.ActionFamily,
            entry.EntityType,
            entry.EntityId,
            entry.AgendaType,
            entry.PacienteId,
            entry.PacienteNombre,
            entry.MedicoId,
            entry.MedicoNombre,
            entry.Title,
            entry.Summary,
            entry.MetadataJson);

    private static string NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
