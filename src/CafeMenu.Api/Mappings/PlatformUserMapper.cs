using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace CafeMenu.Api.Mappings;

[Mapper]
public partial class PlatformUserMapper
{
    [MapperIgnoreSource(nameof(AppUserEntity.PasswordHash))]
    [MapperIgnoreSource(nameof(AppUserEntity.LastLoginAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(AppUserEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.Roles))]
    [MapperIgnoreSource(nameof(AppUserEntity.RefreshTokens))]
    [MapperIgnoreSource(nameof(AppUserEntity.CafeMemberships))]
    public partial PlatformUserResponseDto ToResponse(AppUserEntity user);

    [MapProperty(nameof(AppUserEntity.Id), nameof(PlatformUserSearchResponseDto.AppUserId))]
    [MapperIgnoreSource(nameof(AppUserEntity.PasswordHash))]
    [MapperIgnoreSource(nameof(AppUserEntity.LastLoginAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(AppUserEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.Roles))]
    [MapperIgnoreSource(nameof(AppUserEntity.RefreshTokens))]
    [MapperIgnoreSource(nameof(AppUserEntity.CafeMemberships))]
    public partial PlatformUserSearchResponseDto ToSearchResponse(AppUserEntity user);

    public UserSetupResponseDto ToSetupResponse(
        AppUserEntity user,
        string setupToken,
        DateTimeOffset setupTokenExpiresAt)
    {
        return new UserSetupResponseDto(
            user.Id,
            user.Email,
            user.FullName,
            user.IsActive,
            setupToken,
            setupTokenExpiresAt);
    }
}
