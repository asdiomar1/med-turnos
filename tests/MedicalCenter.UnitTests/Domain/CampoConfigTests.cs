using MedicalCenter.Domain.Entities;

namespace MedicalCenter.UnitTests.Domain;

public sealed class CampoConfigTests
{
    [Fact]
    public void Constructor_AssignsNonEmptyId()
    {
        var campo = new CampoConfig("Peso", "numero", 1);

        Assert.NotEqual(Guid.Empty, campo.Id);
    }
}
