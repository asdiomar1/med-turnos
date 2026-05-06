namespace MedicalCenter.Application.Exceptions;

public sealed class ValidationException(string message, IReadOnlyDictionary<string, string[]>? details = null)
    : ApplicationBaseException(message, "validation_error")
{
    public IReadOnlyDictionary<string, string[]> Details { get; } = details ?? new Dictionary<string, string[]>();
}
