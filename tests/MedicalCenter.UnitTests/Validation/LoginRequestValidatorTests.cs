using MedicalCenter.Contracts.Auth;
using MedicalCenter.Contracts.Validation.Auth;

namespace MedicalCenter.UnitTests.Validation;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyIdentifier_ReturnsError()
    {
        var request = new LoginRequest { Identifier = "", Password = "password123" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Identifier");
    }

    [Fact]
    public void Validate_EmptyPassword_ReturnsError()
    {
        var request = new LoginRequest { Identifier = "admin", Password = "" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new LoginRequest { Identifier = "admin", Password = "password123" };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
