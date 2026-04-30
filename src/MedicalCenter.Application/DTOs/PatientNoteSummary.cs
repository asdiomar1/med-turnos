namespace MedicalCenter.Application.DTOs;

public sealed record PatientNoteSummary(
    Guid Id,
    Guid PatientId,
    Guid AuthorId,
    string Message,
    DateTimeOffset CreatedAt);
