using FluentValidation;
using MedicalCenter.Contracts.Patients;

namespace MedicalCenter.Contracts.Validation.Patients;

public sealed class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("El nombre es obligatorio y no debe exceder 200 caracteres.");

        RuleFor(x => x.Telefono)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("El teléfono es obligatorio y no debe exceder 50 caracteres.");

        RuleFor(x => x.DocumentoIdentidad)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("El documento de identidad es obligatorio.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("El formato de email no es válido.");
    }
}
