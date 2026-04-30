namespace MedicalCenter.Domain.Constants;

public static class ModalidadCobroConstants
{
    public const string Default = "particular";

    public static readonly string[] ValidValues =
    [
        "particular",
        "obra_social",
        "prepaga",
        "convenio"
    ];

    public static bool IsValid(string value) =>
        ValidValues.Contains(value, StringComparer.OrdinalIgnoreCase);
}