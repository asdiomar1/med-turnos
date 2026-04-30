using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class ConfigurationMappings
{
    public static DiasLaborablesConfigDto ToDto(this DiasLaborablesConfig x) =>
        new(x.Id, x.DiasSemana);

    public static WhatsappMessageSettingDto ToDto(this WhatsappMessageSetting x) =>
        new(x.Id, x.Label, x.Description, x.MessageText, x.Active, x.CreatedAt, x.UpdatedAt);

    public static CampoConfigSummaryDto ToSummary(this CampoConfig x) =>
        new(x.Id, x.Nombre, x.Tipo, x.Orden, x.CreatedAt);
}