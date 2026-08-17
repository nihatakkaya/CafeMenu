using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace CafeMenu.Api.Security;

public static class UserSetupTokenGenerator
{
    private const int TokenByteLength = 32;

    public static string Generate()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenByteLength));
    }

    public static string Hash(string token)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
