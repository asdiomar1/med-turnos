using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedicalCenter.Infrastructure.Security;

public sealed class SecurityAuditBackgroundService(
    Channel<SecurityEvent> channel,
    ILogger<SecurityAuditBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in channel.Reader.ReadAllAsync(stoppingToken))
        {
            logger.LogWarning("SecurityEvent: {EventType} | User: {UserId} | Path: {Path} | Message: {Message} | Timestamp: {Timestamp:O}",
                evt.EventType,
                evt.UserId,
                evt.Path,
                evt.Message,
                evt.Timestamp);
        }
    }
}
