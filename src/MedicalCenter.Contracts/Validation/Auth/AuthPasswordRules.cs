using FluentValidation;

namespace MedicalCenter.Contracts.Validation.Auth;

public static class AuthPasswordRules
{
    public const int MinimumPasswordLength = 8;
    public const string MinimumLengthMessage = "La clave debe tener al menos 8 caracteres.";
    public const string DifferentCurrentMessage = "La nueva clave debe ser distinta de la actual.";

    public static bool HasMinimumLength(string? password) =>
        !string.IsNullOrEmpty(password) && password.Length >= MinimumPasswordLength;

    public static bool IsDifferentFromCurrent(string? currentPassword, string? newPassword) =>
        !string.Equals(currentPassword, newPassword, StringComparison.Ordinal);

    public static IRuleBuilderOptions<T, string> RequireMinimumLength<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .MinimumLength(MinimumPasswordLength)
            .WithMessage(MinimumLengthMessage);
    }

    public static IRuleBuilderOptions<T, string> RequireDifferentPassword<T>(this IRuleBuilder<T, string> ruleBuilder, Func<T, string> currentPasswordProvider)
    {
        return ruleBuilder
            .Must((request, newPassword) => IsDifferentFromCurrent(currentPasswordProvider(request), newPassword))
            .WithMessage(DifferentCurrentMessage);
    }
}
