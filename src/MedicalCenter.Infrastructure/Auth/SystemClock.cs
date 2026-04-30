using MedicalCenter.Application.Abstractions.Common;

namespace MedicalCenter.Infrastructure.Auth;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
