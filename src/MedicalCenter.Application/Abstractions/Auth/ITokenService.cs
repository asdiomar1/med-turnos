using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Auth;

public interface ITokenService
{
    TokenEnvelope CreateAccessToken(User user);
    string CreateRefreshTokenValue();
    string CreateNumericCode(int digits);
    string ComputeRefreshTokenHash(string refreshToken);
}
