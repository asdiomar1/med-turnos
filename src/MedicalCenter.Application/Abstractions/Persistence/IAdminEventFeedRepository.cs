using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IAdminEventFeedRepository
{
    Task AddAsync(AdminEventFeedEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminEventFeedEntry>> ListAsync(
        int limit,
        DateTimeOffset? beforeOccurredAt,
        long? beforeId,
        Guid? actorUserId,
        IReadOnlyCollection<string> actionCodes,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken cancellationToken);

    Task<AdminEventFeedFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken);
}
