using MedicalCenter.Contracts.Patients;
using MedicalCenter.Contracts.Validation.Patients;

namespace MedicalCenter.UnitTests.Validation;

public sealed class CreatePatientRequestValidatorTests
{
    private readonly CreatePatientRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyNombre_ReturnsError()
    {
        var request = new CreatePatientRequest { Nombre = "", Telefono = "123", DocumentoIdentidad = "123" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Validate_NombreTooLong_ReturnsError()
    {
        var request = new CreatePatientRequest { Nombre = new string('a', 201), Telefono = "123", DocumentoIdentidad = "123" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsError()
    {
        var request = new CreatePatientRequest { Nombre = "Test", Telefono = "123", DocumentoIdentidad = "123", Email = "not-an-email" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new CreatePatientRequest { Nombre = "Juan Pérez", Telefono = "+54 11 1234 5678", DocumentoIdentidad = "12345678" };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
