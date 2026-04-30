using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class UserPreferenceMappings
{
    public static UserPreferencesSummary ToSummary(this UserPreference x) =>
        new(x.UserId, x.Theme, x.CustomColorsJson, x.TurnosLayout, x.FontScale, x.CreatedAt, x.UpdatedAt);
}