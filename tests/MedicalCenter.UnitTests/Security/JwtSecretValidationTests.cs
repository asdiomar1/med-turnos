using MedicalCenter.Infrastructure.Options;

namespace MedicalCenter.UnitTests.Security;

public sealed class JwtSecretValidationTests
{
    [Fact]
    public void SecretKey_LessThan32Chars_IsWeak()
    {
        var options = new JwtOptions { SecretKey = "short" };

        var isWeak = IsWeakSecret(options.SecretKey);

        Assert.True(isWeak);
    }

    [Fact]
    public void SecretKey_DefaultValue_IsWeak()
    {
        var options = new JwtOptions();

        var isWeak = IsWeakSecret(options.SecretKey);

        Assert.True(isWeak);
    }

    [Fact]
    public void SecretKey_32RandomChars_IsStrong()
    {
        var options = new JwtOptions { SecretKey = new string('x', 32) };

        var isWeak = IsWeakSecret(options.SecretKey);

        Assert.False(isWeak);
    }

    private static bool IsWeakSecret(string secret)
    {
        return secret.Length < 32 || secret.Contains("change-this-secret");
    }
}
