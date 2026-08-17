using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Security;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(AppUserEntity user);

    RefreshTokenResult CreateRefreshToken(long appUserId, DateTimeOffset utcNow);
}
