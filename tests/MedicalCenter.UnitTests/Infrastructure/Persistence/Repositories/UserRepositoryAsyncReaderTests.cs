using System.Data;
using System.Data.Common;
using System.Reflection;
using MedicalCenter.Infrastructure.Persistence.Repositories;

namespace MedicalCenter.UnitTests.Infrastructure.Persistence.Repositories;

public sealed class UserRepositoryAsyncReaderTests
{
    private static readonly string[] DefaultPermissions = ["a", "b"];

    [Fact]
    public async Task ReadPermissionsAsync_WhenPermissionsIsNull_ReturnsEmptyArray()
    {
        using var reader = CreateReader(description: "desc", permissions: DBNull.Value);
        Assert.True(reader.Read());

        var permissions = await InvokePrivateAsync<string[]>("ReadPermissionsAsync", reader);

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task ReadDescriptionAsync_WhenDescriptionHasValue_ReturnsDescription()
    {
        using var reader = CreateReader(description: "description", permissions: DefaultPermissions);
        Assert.True(reader.Read());

        var description = await InvokePrivateAsync<string?>("ReadDescriptionAsync", reader);

        Assert.Equal("description", description);
    }

    private static DataTableReader CreateReader(object description, object permissions)
    {
        var table = new DataTable();
        table.Columns.Add("slug", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("description", typeof(string));
        table.Columns.Add("active", typeof(bool));
        table.Columns.Add("is_system", typeof(bool));
        table.Columns.Add("is_staff", typeof(bool));
        table.Columns.Add("default_home", typeof(string));
        table.Columns.Add("permissions", typeof(string[]));
        table.Rows.Add("admin", "Administrador", description, true, false, true, "home", permissions);
        return table.CreateDataReader();
    }

    private static async Task<T> InvokePrivateAsync<T>(string methodName, DbDataReader reader)
    {
        var method = typeof(UserRepository).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = (Task)method!.Invoke(null, [reader, CancellationToken.None])!;
        await task;

        return (T)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }
}
