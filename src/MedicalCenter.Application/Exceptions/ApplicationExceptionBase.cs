namespace MedicalCenter.Application.Exceptions;

public abstract class ApplicationExceptionBase(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}
