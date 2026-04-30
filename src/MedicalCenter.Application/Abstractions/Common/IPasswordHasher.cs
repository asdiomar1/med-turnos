namespace MedicalCenter.Application.Abstractions.Common;

public interface IPasswordHasher
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}
