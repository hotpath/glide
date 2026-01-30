using System;
using System.Security.Cryptography;

namespace Glide.Web.Auth;

public static class Csrf
{
    public static string GenerateState()
    {
        byte[] randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToHexStringLower(randomBytes);
    }

    public static bool ValidateState(string expected, string actual)
    {
        byte[] expectedBytes = Convert.FromHexString(expected);
        byte[] actualBytes = Convert.FromHexString(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}