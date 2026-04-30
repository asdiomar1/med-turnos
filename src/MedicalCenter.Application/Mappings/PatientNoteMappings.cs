using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class PatientNoteMappings
{
    public static PatientNoteSummary ToSummary(this PatientNote x) =>
        new(x.Id, x.PatientId, x.AuthorId, x.Message, x.CreatedAt);
}