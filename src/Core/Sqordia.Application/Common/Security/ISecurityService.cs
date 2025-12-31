namespace Sqordia.Application.Common.Security;

/// <summary>
/// Security service interface for password hashing and token generation
/// </summary>
public interface ISecurityService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    string GenerateSecureToken(int length = 32);
}
