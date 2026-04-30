namespace MedicalCenter.Application.Exceptions;

public sealed class ConflictException(string message, string code = "conflict")
    : ApplicationExceptionBase(message, code);
