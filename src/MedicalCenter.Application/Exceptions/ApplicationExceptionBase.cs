namespace MedicalCenter.Application.Exceptions;

public abstract class ApplicationBaseException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}
