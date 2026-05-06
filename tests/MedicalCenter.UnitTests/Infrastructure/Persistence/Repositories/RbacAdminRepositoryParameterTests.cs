using System.Reflection;
using MedicalCenter.Infrastructure.Persistence.Repositories;
using Npgsql;

namespace MedicalCenter.UnitTests.Infrastructure.Persistence.Repositories;

public sealed class RbacAdminRepositoryParameterTests
{
    [Fact]
    public void CreateTextArrayParameter_ConfiguresTextArrayType()
    {
        var method = typeof(RbacAdminRepository).GetMethod(
            "CreateTextArrayParameter",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var values = new[] { "admin", "staff" };
        var parameter = Assert.IsType<NpgsqlParameter<string[]>>(method!.Invoke(null, ["roleSlugs", values]));

        Assert.Equal("roleSlugs", parameter.ParameterName);
        Assert.Equal("text[]", parameter.DataTypeName);
        Assert.Equal(values, Assert.IsType<string[]>(parameter.Value));
    }
}
