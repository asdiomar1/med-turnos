using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Abstractions.Auth;

public interface IAuthService
{
    Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(string identifier, string password, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<EffectiveAccessResult> GetEffectiveAccessAsync(Guid userId, CancellationToken cancellationToken);
    Task<PortalActivationResult> ActivatePortalAsync(string token, string loginIdentifier, string password, CancellationToken cancellationToken);
    Task<PortalRecoveryResult> RequestPortalRecoveryAsync(string documentoIdentidad, CancellationToken cancellationToken);
    Task<PortalAccessTokenResult> CreatePortalAccessTokenAsync(Guid pacienteId, string purpose, string deliveryChannel, Guid? issuedBy, CancellationToken cancellationToken);
}
