using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Security;

public sealed record RefreshTokenResult(string Token, RefreshTokenEntity Entity);
