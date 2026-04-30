using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class UserPreferenceRepository(MedicalCenterDbContext dbContext) : IUserPreferenceRepository
{
    public Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task AddAsync(UserPreference preference, CancellationToken cancellationToken) =>
        dbContext.UserPreferences.AddAsync(preference, cancellationToken).AsTask();
}
