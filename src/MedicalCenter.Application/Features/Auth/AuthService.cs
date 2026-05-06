using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Contracts.Validation.Auth;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.Auth;

public sealed record AuthServiceDependencies(
    IUserRepository UserRepository,
    IRoleRepository RoleRepository,
    IRefreshTokenRepository RefreshTokenRepository,
    IPatientRepository PatientRepository,
    IPortalAccessTokenRepository PortalAccessTokenRepository,
    ITokenService TokenService,
    IPasswordHasher PasswordHasher,
    IClock Clock,
    IUnitOfWork UnitOfWork);

public sealed class AuthService(AuthServiceDependencies deps) : IAuthService
{
    private readonly IUserRepository userRepository = deps.UserRepository;
    private readonly IRoleRepository roleRepository = deps.RoleRepository;
    private readonly IRefreshTokenRepository refreshTokenRepository = deps.RefreshTokenRepository;
    private readonly IPatientRepository patientRepository = deps.PatientRepository;
    private readonly IPortalAccessTokenRepository portalAccessTokenRepository = deps.PortalAccessTokenRepository;
    private readonly ITokenService tokenService = deps.TokenService;
    private readonly IPasswordHasher passwordHasher = deps.PasswordHasher;
    private readonly IClock clock = deps.Clock;
    private readonly IUnitOfWork unitOfWork = deps.UnitOfWork;

    private const int PortalAccessTokenDigits = 6;
    private const int PortalAccessTokenGenerationAttempts = 20;
    private const int PortalAccessTokenMaxAttempts = 5;
    private const string ResetPurpose = "reset";
    private const string ManualDeliveryChannel = "manual";
    private static readonly TimeSpan PortalActivationTokenLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan PortalResetTokenLifetime = TimeSpan.FromHours(24);

    public async Task<AuthResponse> LoginAsync(string identifier, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("identifier y password son obligatorios");
        }

        var normalizedIdentifier = NormalizeIdentifier(identifier);
        var user = await userRepository.GetByIdentifierAsync(normalizedIdentifier, cancellationToken);
        if (user is null)
        {
            user = await userRepository.GetByEmailAsync(normalizedIdentifier, cancellationToken);
        }

        if (user is null)
        {
            var patient = await patientRepository.GetByPortalIdentifierAsync(normalizedIdentifier, cancellationToken);
            if (patient is not null)
            {
                user = await userRepository.GetByPatientIdAsync(patient.Id, cancellationToken);
            }
        }

        if (user is null || !user.IsActive || !passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedException();
        }

        await EnsureUserCanAccessPortalAsync(user, cancellationToken);
        await EnsurePortalPatientLinkAsync(user, cancellationToken);
        var roles = await roleRepository.GetByUserIdAsync(user.Id, cancellationToken);
        user.SetRoles(roles);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        ValidatePasswordChangeRequest(currentPassword, newPassword);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException();
        }

        if (!passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException();
        }

        user.SetPasswordHash(passwordHasher.Hash(newPassword));
        await refreshTokenRepository.RevokeActiveByUserIdAsync(userId, clock.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException("refresh_token es obligatorio");
        }

        var tokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var currentToken = await refreshTokenRepository.GetActiveByTokenHashAsync(tokenHash, cancellationToken);
        if (currentToken is null || !currentToken.IsActive(clock.UtcNow))
        {
            throw new UnauthorizedException();
        }

        var user = await userRepository.GetByIdAsync(currentToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException();
        }

        await EnsureUserCanAccessPortalAsync(user, cancellationToken);
        await EnsurePortalPatientLinkAsync(user, cancellationToken);
        var roles = await roleRepository.GetByUserIdAsync(user.Id, cancellationToken);
        user.SetRoles(roles);

        var nextTokenId = Guid.NewGuid();
        currentToken.Rotate(nextTokenId, clock.UtcNow);
        var response = await CreateAuthResponseAsync(user, nextTokenId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException("refresh_token es obligatorio");
        }

        var tokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var currentToken = await refreshTokenRepository.GetActiveByTokenHashAsync(tokenHash, cancellationToken);
        if (currentToken is null)
        {
            return;
        }

        currentToken.Revoke(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<EffectiveAccessResult> GetEffectiveAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
        var roles = await roleRepository.GetByUserIdAsync(userId, cancellationToken);
        user.SetRoles(roles);
        var profileId = await userRepository.GetProfileIdByAuthUserIdAsync(userId, cancellationToken) ?? user.Id;

        var primaryRole = roles.FirstOrDefault();
        var isStaff = roles.Any(x => x.IsStaff);

        return new EffectiveAccessResult(
            profileId,
            roles.Select(x => x.Code).ToArray(),
            roles.SelectMany(x => x.Permissions).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            primaryRole?.Code ?? "user",
            primaryRole?.DefaultHome ?? (isStaff ? "/usuario" : "/paciente"),
            isStaff);
    }

    public async Task<PortalActivationResult> ActivatePortalAsync(string token, string loginIdentifier, string password, CancellationToken cancellationToken)
    {
        ValidatePortalActivationCredentials(token, password);
        var normalizedLoginIdentifier = NormalizeRequiredIdentifier(loginIdentifier, "login_identifier es obligatorio");
        var now = clock.UtcNow;

        var accessToken = await GetUsablePortalActivationTokenAsync(token, now, cancellationToken);
        var patient = await GetPortalActivationPatientAsync(accessToken, now, cancellationToken);
        await EnsurePortalActivationLoginIsAvailableAsync(normalizedLoginIdentifier, patient, accessToken, now, cancellationToken);
        await UpsertPortalUserAsync(patient, normalizedLoginIdentifier, password, cancellationToken);

        CompletePortalActivation(accessToken, patient, normalizedLoginIdentifier);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PortalActivationResult(true, normalizedLoginIdentifier);
    }

    public async Task<PortalRecoveryResult> RequestPortalRecoveryAsync(string documentoIdentidad, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentoIdentidad))
        {
            throw new ValidationException("documento_identidad es obligatorio");
        }

        var patient = await patientRepository.GetByDocumentoAsync(documentoIdentidad.Trim(), cancellationToken);
        if (patient is null || !patient.IsActive || !patient.PortalHabilitado)
        {
            return new PortalRecoveryResult(true, true);
        }

        patient.MarkResetRequired();
        var token = new PortalAccessToken(
            Guid.NewGuid(),
            patient.Id,
            ResetPurpose,
            ManualDeliveryChannel,
            tokenService.ComputeRefreshTokenHash(tokenService.CreateNumericCode(PortalAccessTokenDigits)),
            clock.UtcNow.Add(GetPortalAccessTokenLifetime(ResetPurpose)),
            null);
        token.SetIssuedToMasked(BuildIssuedToMasked(patient, ManualDeliveryChannel));
        await portalAccessTokenRepository.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PortalRecoveryResult(true, false);
    }

    public async Task<PortalAccessTokenResult> CreatePortalAccessTokenAsync(Guid pacienteId, string purpose, string deliveryChannel, Guid? issuedBy, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(pacienteId, cancellationToken)
                      ?? throw new NotFoundException("Paciente no encontrado");
        if (!patient.IsActive)
        {
            throw new ConflictException("Paciente inactivo");
        }

        var normalizedPurpose = purpose.Trim().ToLowerInvariant();
        if (normalizedPurpose is not "activation" and not ResetPurpose)
        {
            throw new ValidationException("purpose invalido");
        }

        var normalizedDeliveryChannel = deliveryChannel.Trim().ToLowerInvariant();
        if (normalizedDeliveryChannel is not ManualDeliveryChannel and not "whatsapp" and not "email")
        {
            throw new ValidationException("delivery_channel invalido");
        }

        if (!patient.PortalHabilitado && string.Equals(normalizedPurpose, ResetPurpose, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("No se puede emitir reset para un paciente sin portal habilitado");
        }

        var tokenResult = await CreatePortalAccessTokenWithRetriesAsync(patient, normalizedPurpose, normalizedDeliveryChannel, issuedBy, cancellationToken);
        return new PortalAccessTokenResult(
            tokenResult.Token.Id,
            tokenResult.Token.Purpose,
            tokenResult.Token.DeliveryChannel,
            tokenResult.Token.ExpiresAt,
            string.Equals(normalizedDeliveryChannel, ManualDeliveryChannel, StringComparison.OrdinalIgnoreCase) ? tokenResult.PlainCode : null);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, Guid refreshTokenId, CancellationToken cancellationToken)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refreshTokenValue = tokenService.CreateRefreshTokenValue();
        var refreshToken = new RefreshToken(
            refreshTokenId,
            user.Id,
            tokenService.ComputeRefreshTokenHash(refreshTokenValue),
            clock.UtcNow.AddDays(7),
            accessToken.JwtId);

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshTokenValue, user.Id, user.Email);
    }

    private Task<AuthResponse> CreateAuthResponseAsync(User user, CancellationToken cancellationToken) =>
        CreateAuthResponseAsync(user, Guid.NewGuid(), cancellationToken);

    private async Task EnsurePortalPatientLinkAsync(User user, CancellationToken cancellationToken)
    {
        if (user.IsStaff || user.PatientId.HasValue)
        {
            return;
        }

        var patient = await patientRepository.GetByPortalIdentifierAsync(user.Identifier, cancellationToken);
        if (patient is null || !patient.IsActive)
        {
            patient = await patientRepository.GetByPortalIdentifierAsync(user.Email, cancellationToken);
        }

        if (patient is null || !patient.IsActive)
        {
            return;
        }

        user.LinkPatient(patient.Id);
    }

    private async Task EnsureUserCanAccessPortalAsync(User user, CancellationToken cancellationToken)
    {
        if (user.IsStaff)
        {
            return;
        }

        if (user.PatientId.HasValue)
        {
            var linkedPatient = await patientRepository.GetByIdAsync(user.PatientId.Value, cancellationToken);
            if (linkedPatient is null || !linkedPatient.IsActive || !linkedPatient.PortalHabilitado)
            {
                throw new UnauthorizedException();
            }

            return;
        }

        var patient = await patientRepository.GetByPortalIdentifierAsync(user.Identifier, cancellationToken)
                      ?? await patientRepository.GetByPortalIdentifierAsync(user.Email, cancellationToken);
        if (patient is null || !patient.IsActive || !patient.PortalHabilitado)
        {
            throw new UnauthorizedException();
        }

        user.LinkPatient(patient.Id);
    }

    private async Task<(PortalAccessToken Token, string PlainCode)> CreatePortalAccessTokenWithRetriesAsync(
        Patient patient,
        string purpose,
        string deliveryChannel,
        Guid? issuedBy,
        CancellationToken cancellationToken)
    {
        var expiresAt = clock.UtcNow.Add(GetPortalAccessTokenLifetime(purpose));
        for (var attempt = 0; attempt < PortalAccessTokenGenerationAttempts; attempt++)
        {
            var plainCode = tokenService.CreateNumericCode(PortalAccessTokenDigits);
            var tokenHash = tokenService.ComputeRefreshTokenHash(plainCode);
            var existing = await portalAccessTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (existing is not null)
            {
                if (existing.IsUsable(clock.UtcNow))
                {
                    continue;
                }

                if (existing.UsedAt is null && existing.RevokedAt is null && existing.ExpiresAt <= clock.UtcNow)
                {
                    existing.Revoke(clock.UtcNow);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            var token = new PortalAccessToken(
                Guid.NewGuid(),
                patient.Id,
                purpose,
                deliveryChannel,
                tokenHash,
                expiresAt,
                issuedBy);
            token.SetIssuedToMasked(BuildIssuedToMasked(patient, deliveryChannel));
            await portalAccessTokenRepository.AddAsync(token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return (token, plainCode);
        }

        throw new ConflictException("No se pudo generar un codigo disponible");
    }

    private async Task RegisterFailedPortalActivationAttemptAsync(PortalAccessToken accessToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        accessToken.RegisterAttempt(now);
        if (accessToken.AttemptCount >= PortalAccessTokenMaxAttempts)
        {
            accessToken.Revoke(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePortalActivationCredentials(string token, string password)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("token y password son obligatorios");
        }

        if (!AuthPasswordRules.HasMinimumLength(password))
        {
            throw new ValidationException(AuthPasswordRules.MinimumLengthMessage);
        }
    }

    private static void ValidatePasswordChangeRequest(string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ValidationException("current_password y new_password son obligatorios");
        }

        if (!AuthPasswordRules.HasMinimumLength(newPassword))
        {
            throw new ValidationException(AuthPasswordRules.MinimumLengthMessage);
        }

        if (!AuthPasswordRules.IsDifferentFromCurrent(currentPassword, newPassword))
        {
            throw new ValidationException(AuthPasswordRules.DifferentCurrentMessage);
        }
    }

    private static string NormalizeRequiredIdentifier(string? rawIdentifier, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(rawIdentifier))
        {
            throw new ValidationException(errorMessage);
        }

        return NormalizeIdentifier(rawIdentifier);
    }

    private async Task<PortalAccessToken> GetUsablePortalActivationTokenAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.ComputeRefreshTokenHash(NormalizeActivationToken(token));
        var accessToken = await portalAccessTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (accessToken is null)
        {
            throw new UnauthorizedException("Token invalido o expirado");
        }

        if (accessToken.IsUsable(now))
        {
            return accessToken;
        }

        if (ShouldRevokeExpiredToken(accessToken, now))
        {
            accessToken.Revoke(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        throw new UnauthorizedException("Token invalido o expirado");
    }

    private async Task<Patient> GetPortalActivationPatientAsync(PortalAccessToken accessToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(accessToken.PacienteId, cancellationToken);
        if (patient is null)
        {
            await RegisterFailedPortalActivationAttemptAsync(accessToken, now, cancellationToken);
            throw new NotFoundException("Paciente no encontrado");
        }

        if (patient.PortalHabilitado)
        {
            return patient;
        }

        await RegisterFailedPortalActivationAttemptAsync(accessToken, now, cancellationToken);
        throw new ConflictException("El paciente no tiene portal habilitado");
    }

    private async Task EnsurePortalActivationLoginIsAvailableAsync(
        string normalizedLoginIdentifier,
        Patient patient,
        PortalAccessToken accessToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingPatientWithLogin = await patientRepository.GetByLoginIdentifierAsync(normalizedLoginIdentifier, cancellationToken);
        if (existingPatientWithLogin is not null && existingPatientWithLogin.Id != patient.Id)
        {
            await RegisterFailedPortalActivationAttemptAsync(accessToken, now, cancellationToken);
            throw new ConflictException("login_identifier ya existe");
        }

        var existingUserWithLogin = await userRepository.GetByIdentifierAsync(normalizedLoginIdentifier, cancellationToken);
        if (existingUserWithLogin is not null && existingUserWithLogin.PatientId != patient.Id)
        {
            await RegisterFailedPortalActivationAttemptAsync(accessToken, now, cancellationToken);
            throw new ConflictException("login_identifier ya existe");
        }
    }

    private async Task UpsertPortalUserAsync(Patient patient, string normalizedLoginIdentifier, string password, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByPatientIdAsync(patient.Id, cancellationToken);
        var passwordHash = passwordHasher.Hash(password);
        if (user is null)
        {
            var contactEmail = ResolveContactEmail(patient);
            user = new User(new UserCreateParams(
                Guid.NewGuid(),
                normalizedLoginIdentifier,
                contactEmail,
                passwordHash,
                true,
                false,
                patient.Id,
                patient.Nombre));
            await userRepository.AddAsync(user, cancellationToken);
            return;
        }

        user.ActivatePortalUser(normalizedLoginIdentifier, passwordHash, ResolveContactEmail(patient));
        user.LinkPatient(patient.Id);
    }

    private void CompletePortalActivation(PortalAccessToken accessToken, Patient patient, string normalizedLoginIdentifier)
    {
        patient.ConfigurePortal(true, false);
        patient.SetLoginIdentifier(normalizedLoginIdentifier);
        accessToken.MarkUsed(clock.UtcNow);
    }

    private static bool ShouldRevokeExpiredToken(PortalAccessToken accessToken, DateTimeOffset now) =>
        accessToken.UsedAt is null && accessToken.RevokedAt is null && accessToken.ExpiresAt <= now;

    private static string ResolveContactEmail(Patient patient)
    {
        if (!string.IsNullOrWhiteSpace(patient.Email))
        {
            return patient.Email.Trim().ToLowerInvariant();
        }

        return $"paciente+{patient.Id:N}@portal.local";
    }

    private static string? BuildIssuedToMasked(Patient patient, string deliveryChannel)
    {
        var channel = deliveryChannel.Trim().ToLowerInvariant();
        return channel switch
        {
            "whatsapp" => MaskValue(patient.Telefono),
            "email" => MaskEmail(patient.Email),
            _ => null
        } ?? string.Empty;
    }

    private static string MaskValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 4 ? trimmed : $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var trimmed = email.Trim();
        var atIndex = trimmed.IndexOf('@');
        if (atIndex <= 1)
        {
            return MaskValue(trimmed);
        }

        return $"{trimmed[0]}***{trimmed[atIndex - 1]}{trimmed[atIndex..]}";
    }

    private static string NormalizeIdentifier(string value) =>
        value.Trim().ToLowerInvariant();

    private static string NormalizeActivationToken(string token) =>
        token.Trim().Replace(" ", string.Empty);

    private static TimeSpan GetPortalAccessTokenLifetime(string purpose) =>
        string.Equals(purpose, "activation", StringComparison.OrdinalIgnoreCase)
            ? PortalActivationTokenLifetime
            : PortalResetTokenLifetime;

}
