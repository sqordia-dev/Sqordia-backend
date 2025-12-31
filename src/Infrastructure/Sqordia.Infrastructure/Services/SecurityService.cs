using Sqordia.Application.Common.Security;
using System.Security.Cryptography;

namespace Sqordia.Infrastructure.Services;

public class SecurityService : ISecurityService
{
    public string HashPassword(string password)
    {
        // BCrypt automatically handles salt generation
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        // Handle null inputs gracefully (but allow empty strings for BCrypt to handle)
        if (password == null || passwordHash == null)
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateSecureToken(int length = 32)
    {
        var bytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}
