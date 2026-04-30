namespace MedicalCenter.Application.Exceptions;

public sealed class UnauthorizedException(string message = "No autorizado")
    : ApplicationExceptionBase(message, "unauthorized");
