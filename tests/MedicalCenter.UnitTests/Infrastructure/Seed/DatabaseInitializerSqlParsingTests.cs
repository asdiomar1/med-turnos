using System.Reflection;
using MedicalCenter.Infrastructure.Seed;

namespace MedicalCenter.UnitTests.Infrastructure.Seed;

public sealed class DatabaseInitializerSqlParsingTests
{
    [Fact]
    public void ShouldSkipSqlLine_ReturnsTrue_ForMetadataLine()
    {
        var method = GetShouldSkipSqlLineMethod();
        var result = Assert.IsType<bool>(method.Invoke(null, ["Type: TABLE DATA;"]));
        Assert.True(result);
    }

    [Fact]
    public void ShouldSkipSqlLine_ReturnsTrue_ForCommentLine()
    {
        var method = GetShouldSkipSqlLineMethod();
        var result = Assert.IsType<bool>(method.Invoke(null, ["-- comment"]));
        Assert.True(result);
    }

    [Fact]
    public void ShouldSkipSqlLine_ReturnsFalse_ForValidSqlLine()
    {
        var method = GetShouldSkipSqlLineMethod();
        var result = Assert.IsType<bool>(method.Invoke(null, ["INSERT INTO test VALUES (1);"]));
        Assert.False(result);
    }

    private static MethodInfo GetShouldSkipSqlLineMethod()
    {
        var method = typeof(DatabaseInitializer).GetMethod(
            "ShouldSkipSqlLine",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return method!;
    }
}
