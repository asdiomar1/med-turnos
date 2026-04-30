using MedicalCenter.Infrastructure.Auth;

namespace MedicalCenter.UnitTests.Auth;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_ReturnsTrue()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("Admin123!");

        var result = hasher.Verify("Admin123!", hash);

        Assert.True(result);
    }
}
