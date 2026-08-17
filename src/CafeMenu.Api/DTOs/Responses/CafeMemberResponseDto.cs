namespace CafeMenu.Api.DTOs.Responses;

public sealed record CafeMemberResponseDto(
    long MembershipId,
    long AppUserId,
    string Email,
    string FullName,
    string RoleCode,
    bool IsActive);
