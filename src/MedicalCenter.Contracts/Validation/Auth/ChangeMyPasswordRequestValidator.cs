using FluentValidation;
using MedicalCenter.Contracts.Auth;

namespace MedicalCenter.Contracts.Validation.Auth;

public sealed class ChangeMyPasswordRequestValidator : AbstractValidator<ChangeMyPasswordRequest>
{
    public ChangeMyPasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("La contraseña actual es obligatoria.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("La nueva contraseña es obligatoria.")
            .RequireMinimumLength()
            .RequireDifferentPassword(x => x.CurrentPassword);
    }
}
