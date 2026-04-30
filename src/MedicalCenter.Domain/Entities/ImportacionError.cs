using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class ImportacionError : Entity<long>
{
    private ImportacionError() { }

    public ImportacionError(Guid importacionId, int rowNumber, string message, string? field = null)
    {
        ImportacionId = importacionId;
        RowNumber = rowNumber;
        Message = message;
        Field = field;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid ImportacionId { get; private set; }
    public int RowNumber { get; private set; }
    public string? Field { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
