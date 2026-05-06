using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Auth;
using MedicalCenter.Contracts.Validation.Auth;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Auth;

public sealed class AuthServiceChangePasswordTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly IPortalAccessTokenRepository _portalAccessTokenRepository = Substitute.For<IPortalAccessTokenRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task ChangeOwnPasswordAsync_WhenUserDoesNotExist_ThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid();
        var sut = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.ChangeOwnPasswordAsync(userId, "actual-123", "nueva-clave-123", CancellationToken.None));

        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().RevokeActiveByUserIdAsync(default, default, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task ChangeOwnPasswordAsync_WhenUserIsInactive_ThrowsUnauthorizedException()
    {
        var user = BuildUser(isActive: false);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var sut = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.ChangeOwnPasswordAsync(user.Id, "actual-123", "nueva-clave-123", CancellationToken.None));

        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().RevokeActiveByUserIdAsync(default, default, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WhenCurrentPasswordIsInvalid_ThrowsUnauthorizedException()
    {
        var user = BuildUser(isActive: true);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("actual-123", user.PasswordHash).Returns(false);
        var sut = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.ChangeOwnPasswordAsync(user.Id, "actual-123", "nueva-clave-123", CancellationToken.None));

        Assert.Equal("stored-hash", user.PasswordHash);
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().RevokeActiveByUserIdAsync(default, default, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WhenNewPasswordIsTooShort_ThrowsValidationExceptionWithSharedRuleMessage()
    {
        var sut = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangeOwnPasswordAsync(Guid.NewGuid(), "actual-123", "short", CancellationToken.None));

        Assert.Equal(AuthPasswordRules.MinimumLengthMessage, exception.Message);
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().RevokeActiveByUserIdAsync(default, default, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WhenNewPasswordMatchesCurrent_ThrowsValidationExceptionWithSharedRuleMessage()
    {
        var sut = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangeOwnPasswordAsync(Guid.NewGuid(), "misma-clave-123", "misma-clave-123", CancellationToken.None));

        Assert.Equal(AuthPasswordRules.DifferentPasswordMessage, exception.Message);
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().RevokeActiveByUserIdAsync(default, default, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WhenRequestIsValid_UpdatesHashRevokesTokensAndSavesOnce()
    {
        var user = BuildUser(isActive: true);
        var now = new DateTimeOffset(2026, 5, 5, 20, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(now);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("actual-123", user.PasswordHash).Returns(true);
        _passwordHasher.Hash("nueva-clave-123").Returns("new-hash");
        var sut = CreateService();

        await sut.ChangeOwnPasswordAsync(user.Id, "actual-123", "nueva-clave-123", CancellationToken.None);

        Assert.Equal("new-hash", user.PasswordHash);
        await _refreshTokenRepository.Received(1).RevokeActiveByUserIdAsync(user.Id, now, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WhenRefreshTokenRevocationFails_ThrowsAndDoesNotSave()
    {
        var user = BuildUser(isActive: true);
        var now = new DateTimeOffset(2026, 5, 5, 20, 0, 0, TimeSpan.Zero);
        var expected = new InvalidOperationException("db down");
        _clock.UtcNow.Returns(now);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("actual-123", user.PasswordHash).Returns(true);
        _passwordHasher.Hash("nueva-clave-123").Returns("new-hash");
        _refreshTokenRepository
            .When(repository => repository.RevokeActiveByUserIdAsync(user.Id, now, Arg.Any<CancellationToken>()))
            .Do(_ => throw expected);
        var sut = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ChangeOwnPasswordAsync(user.Id, "actual-123", "nueva-clave-123", CancellationToken.None));

        Assert.Same(expected, exception);
        await _refreshTokenRepository.Received(1).RevokeActiveByUserIdAsync(user.Id, now, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private AuthService CreateService()
    {
        return new AuthService(new AuthServiceDependencies(
            _userRepository,
            _roleRepository,
            _refreshTokenRepository,
            _patientRepository,
            _portalAccessTokenRepository,
            _tokenService,
            _passwordHasher,
            _clock,
            _unitOfWork));
    }

    private static User BuildUser(bool isActive)
    {
        return new User(new UserCreateParams(
            Guid.NewGuid(),
            "staff",
            "staff@medicalcenter.local",
            "stored-hash",
            isActive,
            IsStaff: true));
    }
}
