namespace MedicalCenter.Application.Exceptions;

public sealed class NotFoundException(string message = "No encontrado")
    : ApplicationBaseException(message, "not_found");
