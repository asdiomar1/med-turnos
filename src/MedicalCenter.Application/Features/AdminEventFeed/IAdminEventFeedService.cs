using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.AdminEventFeed;

public interface IAdminEventFeedService
{
    Task<IReadOnlyCollection<AdminEventFeedItemDto>> ListAsync(AdminEventFeedQuery query, CancellationToken cancellationToken);

    Task<AdminEventFeedFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken);
}
