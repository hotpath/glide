using System;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Glide.Web.Auth;

public class PasswordAuthService
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000; // OWASP recommendation

    /// <summary>
    /// Hashes a password using PBKDF2 with a random salt
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>Base64 encoded string: salt + hash</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty", nameof(password));
        }

        // Generate a random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Hash the password
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        // Combine salt + hash and encode as base64
        byte[] combined = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The base64 encoded salt + hash</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        try
        {
            // Decode the combined salt + hash
            byte[] combined = Convert.FromBase64String(hashedPassword);

            if (combined.Length != SaltSize + HashSize)
            {
                return false;
            }

            // Extract the salt
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);

            // Extract the hash
            byte[] storedHash = new byte[HashSize];
            Buffer.BlockCopy(combined, SaltSize, storedHash, 0, HashSize);

            // Hash the provided password with the extracted salt
            byte[] computedHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSize);

            // Compare hashes using constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>Error message if invalid, null if valid</returns>
    public string? ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required";
        }

        if (password.Length < 8)
        {
            return "Password must be at least 8 characters long";
        }

        if (password.Length > 128)
        {
            return "Password must not exceed 128 characters";
        }

        // Optional: Add more complexity requirements here
        // For now, just length validation is sufficient

        return null; // Password is valid
    }
}