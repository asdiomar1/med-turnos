using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.UserPreferences;

public interface IUserPreferencesService
{
    Task<UserPreferencesSummary> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserPreferencesSummary> UpsertAsync(UpdateUserPreferencesCommand command, CancellationToken cancellationToken);
}
