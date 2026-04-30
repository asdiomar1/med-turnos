using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class PatientNote : Entity<Guid>
{
    private PatientNote() { }

    public PatientNote(Guid id, Guid patientId, Guid authorId, string message, DateTimeOffset createdAt)
    {
        Id = id;
        PatientId = patientId;
        AuthorId = authorId;
        Message = message;
        CreatedAt = createdAt;
    }

    public Guid PatientId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
