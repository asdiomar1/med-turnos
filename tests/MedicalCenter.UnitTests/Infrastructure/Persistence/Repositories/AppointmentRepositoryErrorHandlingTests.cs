using System.Reflection;
using MedicalCenter.Infrastructure.Persistence.Repositories;

namespace MedicalCenter.UnitTests.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepositoryErrorHandlingTests
{
    [Fact]
    public void CreateTryCommitException_PreservesInnerExceptionAndAddsContext()
    {
        var inner = new InvalidOperationException("db failure");
        var exception = InvokeFactory("CreateTryCommitException", inner);

        Assert.Equal("Unexpected database error during TryCommitAsync.", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void CreateTryCommitWithPatientLockException_PreservesInnerExceptionAndAddsPatientContext()
    {
        var patientId = Guid.NewGuid();
        var inner = new InvalidOperationException("db failure");
        var exception = InvokeFactory("CreateTryCommitWithPatientLockException", patientId, inner);

        Assert.Equal(
            $"Unexpected database error during TryCommitWithPatientLockAsync for patient {patientId}.",
            exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    private static Exception InvokeFactory(string methodName, params object[] args)
    {
        var method = typeof(AppointmentRepository)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return (Exception)method!.Invoke(null, args)!;
    }
}
