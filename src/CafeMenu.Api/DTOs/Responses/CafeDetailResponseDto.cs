namespace CafeMenu.Api.DTOs.Responses;

public sealed record CafeDetailResponseDto(
    long Id,
    string Name,
    string Slug,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<CafeMembershipResponseDto> Memberships);
