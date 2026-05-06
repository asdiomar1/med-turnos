namespace MedicalCenter.Application.Exceptions;

public sealed class ForbiddenException(string message = "Prohibido")
    : ApplicationBaseException(message, "forbidden");
