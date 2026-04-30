using System.Threading.Channels;

namespace MedicalCenter.Infrastructure.Security;

public interface ISecurityAuditLogger
{
    void LogAsync(SecurityEvent securityEvent);
}

public sealed class SecurityAuditLogger(Channel<SecurityEvent> channel) : ISecurityAuditLogger
{
    public void LogAsync(SecurityEvent securityEvent)
    {
        channel.Writer.TryWrite(securityEvent);
    }
}
