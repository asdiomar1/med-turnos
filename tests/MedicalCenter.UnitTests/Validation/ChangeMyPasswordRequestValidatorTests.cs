using MedicalCenter.Contracts.Auth;
using MedicalCenter.Contracts.Validation.Auth;

namespace MedicalCenter.UnitTests.Validation;

public sealed class ChangeMyPasswordRequestValidatorTests
{
    private readonly ChangeMyPasswordRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyPasswords_ReturnsErrors()
    {
        var request = new ChangeMyPasswordRequest();

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CurrentPassword");
        Assert.Contains(result.Errors, error => error.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_NewPasswordShorterThanMinimum_ReturnsError()
    {
        var request = new ChangeMyPasswordRequest
        {
            CurrentPassword = "actual-123",
            NewPassword = "short"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_NewPasswordEqualsCurrent_ReturnsError()
    {
        var request = new ChangeMyPasswordRequest
        {
            CurrentPassword = "misma-clave-123",
            NewPassword = "misma-clave-123"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new ChangeMyPasswordRequest
        {
            CurrentPassword = "actual-123",
            NewPassword = "nueva-clave-123"
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
