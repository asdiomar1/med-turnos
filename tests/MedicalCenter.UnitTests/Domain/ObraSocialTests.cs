using MedicalCenter.Domain.Entities;

namespace MedicalCenter.UnitTests.Domain;

public sealed class ObraSocialTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var sut = new ObraSocial(12, "OSDE", true, true, 3, "os");

        Assert.Equal(12, sut.Id);
        Assert.Equal("OSDE", sut.Nombre);
        Assert.True(sut.Activa);
        Assert.True(sut.TieneConvenio);
        Assert.Equal(3, sut.Orden);
        Assert.Equal("os", sut.Abreviatura);
    }
}
