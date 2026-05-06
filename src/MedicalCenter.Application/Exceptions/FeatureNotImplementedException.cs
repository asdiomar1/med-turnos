namespace MedicalCenter.Application.Exceptions;

public sealed class FeatureNotImplementedException(string message = "Modulo disponible solo como scaffolding inicial")
    : ApplicationBaseException(message, "not_implemented");
