using Microsoft.Extensions.Logging;
using Sqordia.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Sqordia.Infrastructure.Services;

/// <summary>
/// Service for encrypting and decrypting setting values using AES-256-GCM
/// </summary>
public class SettingsEncryptionService : ISettingsEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<SettingsEncryptionService> _logger;
    private readonly bool _isAvailable;

    public SettingsEncryptionService(ILogger<SettingsEncryptionService> logger)
    {
        _logger = logger;

        // Get encryption key from environment variable
        var keyString = Environment.GetEnvironmentVariable("SETTINGS_ENCRYPTION_KEY");
        
        if (string.IsNullOrEmpty(keyString))
        {
            _logger.LogWarning("SETTINGS_ENCRYPTION_KEY not configured. Encryption will not be available.");
            _isAvailable = false;
            _key = Array.Empty<byte>();
            return;
        }

        try
        {
            // Convert base64 string to byte array, or use direct string as key
            if (keyString.Length >= 32)
            {
                // Use first 32 bytes for AES-256
                _key = Encoding.UTF8.GetBytes(keyString.Substring(0, 32));
            }
            else
            {
                // Pad or hash to 32 bytes
                using var sha256 = SHA256.Create();
                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }

            _isAvailable = true;
            _logger.LogInformation("Settings encryption service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize encryption key");
            _isAvailable = false;
            _key = Array.Empty<byte>();
        }
    }

    public string Encrypt(string plainText)
    {
        if (!_isAvailable)
        {
            _logger.LogWarning("Encryption not available. Returning plain text.");
            return plainText;
        }

        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aes = new AesGcm(_key);
            
            // Generate nonce (12 bytes for GCM)
            var nonce = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            // Encrypt
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[16]; // 128-bit authentication tag

            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Combine nonce + tag + ciphertext
            var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting value");
            throw new InvalidOperationException("Failed to encrypt value", ex);
        }
    }

    public string Decrypt(string encryptedValue)
    {
        if (!_isAvailable)
        {
            _logger.LogWarning("Encryption not available. Returning encrypted value as-is.");
            return encryptedValue;
        }

        if (string.IsNullOrEmpty(encryptedValue))
            return encryptedValue;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedValue);

            // Extract nonce, tag, and ciphertext
            if (encryptedBytes.Length < 28) // 12 (nonce) + 16 (tag) minimum
            {
                _logger.LogWarning("Invalid encrypted value format. Returning as-is.");
                return encryptedValue;
            }

            var nonce = new byte[12];
            var tag = new byte[16];
            var cipherBytes = new byte[encryptedBytes.Length - 28];

            Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, 12);
            Buffer.BlockCopy(encryptedBytes, 12, tag, 0, 16);
            Buffer.BlockCopy(encryptedBytes, 28, cipherBytes, 0, cipherBytes.Length);

            // Decrypt
            using var aes = new AesGcm(_key);
            var plainBytes = new byte[cipherBytes.Length];
            
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting value. Value may not be encrypted.");
            // Return as-is if decryption fails (might be plain text)
            return encryptedValue;
        }
    }

    public bool IsEncryptionAvailable()
    {
        return _isAvailable;
    }
}

