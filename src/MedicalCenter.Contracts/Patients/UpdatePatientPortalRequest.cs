using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Patients;

public sealed class UpdatePatientPortalRequest
{
    [JsonPropertyName("portal_habilitado")]
    public bool PortalHabilitado { get; init; }
}
