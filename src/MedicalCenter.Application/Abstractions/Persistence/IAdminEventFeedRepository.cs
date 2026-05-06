using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IAdminEventFeedRepository
{
    Task AddAsync(AdminEventFeedEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminEventFeedEntry>> ListAsync(AdminEventFeedQuery query, CancellationToken cancellationToken);

    Task<AdminEventFeedFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken);
}
