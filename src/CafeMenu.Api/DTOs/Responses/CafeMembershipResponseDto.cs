namespace CafeMenu.Api.DTOs.Responses;

public sealed record CafeMembershipResponseDto(
    long Id,
    long CafeId,
    long AppUserId,
    string UserEmail,
    string UserFullName,
    string RoleCode,
    bool IsActive);
