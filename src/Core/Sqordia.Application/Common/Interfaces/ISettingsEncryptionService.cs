namespace Sqordia.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting and decrypting setting values
/// </summary>
public interface ISettingsEncryptionService
{
    /// <summary>
    /// Encrypt a setting value
    /// </summary>
    /// <param name="plainText">Plain text value to encrypt</param>
    /// <returns>Encrypted value (base64 encoded)</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypt a setting value
    /// </summary>
    /// <param name="encryptedValue">Encrypted value (base64 encoded)</param>
    /// <returns>Decrypted plain text value</returns>
    string Decrypt(string encryptedValue);

    /// <summary>
    /// Check if encryption is configured and available
    /// </summary>
    bool IsEncryptionAvailable();
}

