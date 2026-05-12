using FluentValidation;
using MedicalCenter.Contracts.Consultations;

namespace MedicalCenter.Contracts.Validation.Consultations;

public sealed class OutOfHoursTurnCreateRequestValidator : AbstractValidator<OutOfHoursTurnCreateRequest>
{
    public OutOfHoursTurnCreateRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.EsMonoxido || (x.MonoxidoOrdenMedica && x.MonoxidoResumenClinico))
            .WithMessage("Si es_monoxido es true, monoxido_orden_medica y monoxido_resumen_clinico deben ser true.");
    }
}
