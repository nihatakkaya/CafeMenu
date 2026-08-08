using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CafeMenu.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CafeMenu.Api.Security;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public AccessTokenResult CreateAccessToken(AppUserEntity user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new("app_user_id", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Code)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken(long appUserId, DateTimeOffset utcNow)
    {
        var token = RefreshTokenGenerator.Generate();
        var tokenHash = RefreshTokenGenerator.Hash(token);
        var entity = new RefreshTokenEntity
        {
            AppUserId = appUserId,
            TokenHash = tokenHash,
            ExpiresAt = utcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        return new RefreshTokenResult(token, entity);
    }
}
