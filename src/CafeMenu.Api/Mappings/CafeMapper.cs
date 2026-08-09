using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace CafeMenu.Api.Mappings;

[Mapper]
public partial class CafeMapper
{
    [MapperIgnoreSource(nameof(CafeEntity.LogoImageUrl))]
    [MapperIgnoreSource(nameof(CafeEntity.CoverImageUrl))]
    [MapperIgnoreSource(nameof(CafeEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(CafeEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(CafeEntity.Memberships))]
    [MapperIgnoreSource(nameof(CafeEntity.Categories))]
    [MapperIgnoreSource(nameof(CafeEntity.Products))]
    public partial CafeResponseDto ToResponse(CafeEntity cafe);

    [MapperIgnoreSource(nameof(CafeEntity.LogoImageUrl))]
    [MapperIgnoreSource(nameof(CafeEntity.CoverImageUrl))]
    [MapperIgnoreSource(nameof(CafeEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(CafeEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(CafeEntity.Memberships))]
    [MapperIgnoreSource(nameof(CafeEntity.Categories))]
    [MapperIgnoreSource(nameof(CafeEntity.Products))]
    private partial CafeDetailResponseDto ToDetailResponseInternal(
        CafeEntity cafe,
        IReadOnlyCollection<CafeMembershipResponseDto> memberships);

    public CafeDetailResponseDto ToDetailResponse(CafeEntity cafe)
    {
        return ToDetailResponseInternal(cafe, cafe.Memberships.Select(ToMembershipResponse).ToArray());
    }

    private static CafeMembershipResponseDto ToMembershipResponse(CafeMembershipEntity membership)
    {
        return new CafeMembershipResponseDto(
            membership.Id,
            membership.CafeId,
            membership.AppUserId,
            membership.AppUser.Email,
            membership.AppUser.FullName,
            membership.Role.Code,
            membership.IsActive);
    }
}
