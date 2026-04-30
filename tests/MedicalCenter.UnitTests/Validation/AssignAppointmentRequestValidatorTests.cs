using MedicalCenter.Contracts.Appointments;
using MedicalCenter.Contracts.Validation.Appointments;

namespace MedicalCenter.UnitTests.Validation;

public sealed class AssignAppointmentRequestValidatorTests
{
    private readonly AssignAppointmentRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyPacienteId_ReturnsError()
    {
        var request = new AssignAppointmentRequest { PacienteId = Guid.Empty };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PacienteId");
    }

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new AssignAppointmentRequest { PacienteId = Guid.NewGuid() };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NumeroAutorizacionTooLong_ReturnsError()
    {
        var request = new AssignAppointmentRequest
        {
            PacienteId = Guid.NewGuid(),
            NumeroAutorizacion = new string('a', 101)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NumeroAutorizacion");
    }
}
