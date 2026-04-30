namespace MedicalCenter.Application.Exceptions;

public sealed class NotFoundException(string message = "No encontrado")
    : ApplicationExceptionBase(message, "not_found");
