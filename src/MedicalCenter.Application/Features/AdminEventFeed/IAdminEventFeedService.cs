using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.AdminEventFeed;

public interface IAdminEventFeedService
{
    Task<IReadOnlyCollection<AdminEventFeedItemDto>> ListAsync(
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
