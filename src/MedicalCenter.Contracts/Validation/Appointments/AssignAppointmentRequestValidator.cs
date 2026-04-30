using FluentValidation;
using MedicalCenter.Contracts.Appointments;

namespace MedicalCenter.Contracts.Validation.Appointments;

public sealed class AssignAppointmentRequestValidator : AbstractValidator<AssignAppointmentRequest>
{
    public AssignAppointmentRequestValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty()
            .WithMessage("El paciente es obligatorio.");

        RuleFor(x => x.ModalidadCobro)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.ModalidadCobro))
            .WithMessage("La modalidad de cobro no debe exceder 50 caracteres.");

        RuleFor(x => x.NumeroAutorizacion)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.NumeroAutorizacion))
            .WithMessage("El número de autorización no debe exceder 100 caracteres.");
    }
}
