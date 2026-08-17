using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace CafeMenu.Api.Mappings;

[Mapper]
public partial class AppUserMapper
{
    [MapperIgnoreSource(nameof(AppUserEntity.PasswordHash))]
    [MapperIgnoreSource(nameof(AppUserEntity.IsActive))]
    [MapperIgnoreSource(nameof(AppUserEntity.LastLoginAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(AppUserEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(AppUserEntity.Roles))]
    [MapperIgnoreSource(nameof(AppUserEntity.RefreshTokens))]
    [MapperIgnoreSource(nameof(AppUserEntity.CafeMemberships))]
    private partial UserResponseDto ToResponseInternal(AppUserEntity user, IReadOnlyCollection<string> roles);

    public UserResponseDto ToResponse(AppUserEntity user)
    {
        var roles = user.Roles
            .Select(role => role.Code)
            .OrderBy(role => role)
            .ToArray();

        return ToResponseInternal(user, roles);
    }
}
