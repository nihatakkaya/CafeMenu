using System.Security.Cryptography;

namespace CafeMenu.Api.Security;

public static class RefreshTokenGenerator
{
    private const int TokenByteLength = 64;

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return Convert.ToBase64String(bytes);
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
