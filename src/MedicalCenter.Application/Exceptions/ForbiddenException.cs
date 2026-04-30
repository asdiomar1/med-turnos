namespace MedicalCenter.Application.Exceptions;

public sealed class ForbiddenException(string message = "Prohibido")
    : ApplicationExceptionBase(message, "forbidden");
