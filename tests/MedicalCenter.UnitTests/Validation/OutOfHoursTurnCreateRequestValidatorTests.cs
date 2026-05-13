using MedicalCenter.Contracts.Consultations;
using MedicalCenter.Contracts.Validation.Consultations;

namespace MedicalCenter.UnitTests.Validation;

public sealed class OutOfHoursTurnCreateRequestValidatorTests
{
    private readonly OutOfHoursTurnCreateRequestValidator _validator = new();

    [Fact]
    public void Validate_MonoxidoTrueAndFlagsFalse_ReturnsError()
    {
        var request = new OutOfHoursTurnCreateRequest
        {
            Fecha = new DateOnly(2026, 5, 11),
            Hora = new TimeOnly(18, 0),
            PacienteId = Guid.NewGuid(),
            EsMonoxido = true,
            MonoxidoOrdenMedica = false,
            MonoxidoResumenClinico = true
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_MonoxidoTrueAndFlagsTrue_Passes()
    {
        var request = new OutOfHoursTurnCreateRequest
        {
            Fecha = new DateOnly(2026, 5, 11),
            Hora = new TimeOnly(18, 0),
            PacienteId = Guid.NewGuid(),
            EsMonoxido = true,
            MonoxidoOrdenMedica = true,
            MonoxidoResumenClinico = true
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MonoxidoFalse_IgnoresMonoxidoFlags()
    {
        var request = new OutOfHoursTurnCreateRequest
        {
            Fecha = new DateOnly(2026, 5, 11),
            Hora = new TimeOnly(18, 0),
            PacienteId = Guid.NewGuid(),
            EsMonoxido = false,
            MonoxidoOrdenMedica = false,
            MonoxidoResumenClinico = false
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
