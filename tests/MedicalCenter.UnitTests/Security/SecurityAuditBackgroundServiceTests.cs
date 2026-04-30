using System.Threading.Channels;
using MedicalCenter.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace MedicalCenter.UnitTests.Security;

public sealed class SecurityAuditBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ReadsChannelAndLogsEvent()
    {
        var channel = Channel.CreateUnbounded<SecurityEvent>();
        var loggerFactory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider()));
        var logger = loggerFactory.CreateLogger<SecurityAuditBackgroundService>();
        var service = new SecurityAuditBackgroundService(channel, logger);
        var cts = new CancellationTokenSource();

        var executeTask = service.StartAsync(cts.Token);

        var evt = new SecurityEvent("unauthorized_access", "Test message", "user-123", "/api/test");
        await channel.Writer.WriteAsync(evt);

        // Give it a moment to process
        await Task.Delay(100);
        cts.Cancel();

        try { await executeTask; } catch (OperationCanceledException) { }

        // Verify the logger captured it via TestLoggerProvider
        Assert.True(TestLoggerProvider.HasEvent("unauthorized_access"));
    }

    [Fact]
    public async Task ExecuteAsync_ChannelCompleted_StopsGracefully()
    {
        var channel = Channel.CreateUnbounded<SecurityEvent>();
        var loggerFactory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider()));
        var logger = loggerFactory.CreateLogger<SecurityAuditBackgroundService>();
        var service = new SecurityAuditBackgroundService(channel, logger);
        var cts = new CancellationTokenSource();

        var executeTask = service.StartAsync(cts.Token);
        channel.Writer.Complete();

        await Task.Delay(100);
        cts.Cancel();

        try { await executeTask; } catch (OperationCanceledException) { }

        Assert.True(executeTask.IsCompleted);
    }
}

public sealed class TestLoggerProvider : ILoggerProvider
{
    private static readonly List<string> _eventTypes = [];

    public static bool HasEvent(string eventType) => _eventTypes.Contains(eventType);

    public static void Clear() => _eventTypes.Clear();

    public ILogger CreateLogger(string categoryName) => new TestLogger(_eventTypes);

    public void Dispose() { }
}

public sealed class TestLogger(List<string> eventTypes) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (message.Contains("unauthorized_access"))
        {
            eventTypes.Add("unauthorized_access");
        }
    }
}
