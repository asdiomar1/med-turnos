using System.Threading.Channels;
using MedicalCenter.Infrastructure.Security;

namespace MedicalCenter.UnitTests.Security;

public sealed class SecurityAuditLoggerTests
{
    [Fact]
    public void LogAsync_WritesEventToChannel()
    {
        var channel = Channel.CreateUnbounded<SecurityEvent>();
        var logger = new SecurityAuditLogger(channel);
        var evt = new SecurityEvent("unauthorized_access", "User tried to access another patient's data", "test-user", "/api/v1/pacientes/123");

        logger.LogAsync(evt);

        Assert.True(channel.Reader.TryRead(out var readEvent));
        Assert.Equal("unauthorized_access", readEvent.EventType);
        Assert.Equal("test-user", readEvent.UserId);
    }

    [Fact]
    public void LogAsync_DoesNotBlockCaller()
    {
        var channel = Channel.CreateUnbounded<SecurityEvent>();
        var logger = new SecurityAuditLogger(channel);
        var evt = new SecurityEvent("test", "test", "user", "/test");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        logger.LogAsync(evt);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100, "LogAsync should be non-blocking");
    }
}
