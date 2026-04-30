using System.Text.Json;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.UserPreferences;

public sealed class UserPreferencesService(
    IUserRepository userRepository,
    IUserPreferenceRepository userPreferenceRepository,
    IUnitOfWork unitOfWork) : IUserPreferencesService
{
    public async Task<UserPreferencesSummary> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await EnsureAsync(userId, cancellationToken);
        return preference.ToSummary();
    }

    public async Task<UserPreferencesSummary> UpsertAsync(UpdateUserPreferencesCommand command, CancellationToken cancellationToken)
    {
        var preference = await EnsureAsync(command.UserId, cancellationToken);
        preference.Update(command.Theme, command.CustomColorsJson, command.TurnosLayout, command.FontScale);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return preference.ToSummary();
    }

    private async Task<UserPreference> EnsureAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
        var preference = await userPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (preference is not null)
        {
            return preference;
        }

        preference = new UserPreference(user.Id, "system", null, "standard", 1.0m);
        await userPreferenceRepository.AddAsync(preference, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return preference;
    }

    }
