using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Auth;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Auth;

public sealed class AuthServiceActivationTests
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
    public async Task ActivatePortalAsync_WhenLoginIdentifierIsNull_ThrowsValidationException()
    {
        var sut = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ActivatePortalAsync("123456", null!, "12345678", CancellationToken.None));
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
}
