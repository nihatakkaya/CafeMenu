namespace CafeMenu.Web.AdminPlatform;

public enum AdminPlatformRequestStatus
{
    Success,
    ValidationError,
    UnauthorizedOrForbidden,
    NotFound,
    Conflict,
    Failure
}

public sealed record AdminPlatformCafeListResult(
    AdminPlatformRequestStatus Status,
    IReadOnlyCollection<AdminPlatformCafeResponse> Cafes)
{
    public static AdminPlatformCafeListResult Success(IReadOnlyCollection<AdminPlatformCafeResponse> cafes)
    {
        return new AdminPlatformCafeListResult(AdminPlatformRequestStatus.Success, cafes);
    }

    public static AdminPlatformCafeListResult Failure(AdminPlatformRequestStatus status = AdminPlatformRequestStatus.Failure)
    {
        return new AdminPlatformCafeListResult(status, []);
    }
}

public sealed record AdminPlatformCafeMutationResult(
    AdminPlatformRequestStatus Status,
    AdminPlatformCafeResponse? Cafe)
{
    public static AdminPlatformCafeMutationResult Success(AdminPlatformCafeResponse cafe)
    {
        return new AdminPlatformCafeMutationResult(AdminPlatformRequestStatus.Success, cafe);
    }

    public static AdminPlatformCafeMutationResult Failure(AdminPlatformRequestStatus status = AdminPlatformRequestStatus.Failure)
    {
        return new AdminPlatformCafeMutationResult(status, null);
    }
}

public sealed record AdminPlatformMemberListResult(
    AdminPlatformRequestStatus Status,
    IReadOnlyCollection<AdminPlatformCafeMemberResponse> Members)
{
    public static AdminPlatformMemberListResult Success(IReadOnlyCollection<AdminPlatformCafeMemberResponse> members)
    {
        return new AdminPlatformMemberListResult(AdminPlatformRequestStatus.Success, members);
    }

    public static AdminPlatformMemberListResult Failure(AdminPlatformRequestStatus status = AdminPlatformRequestStatus.Failure)
    {
        return new AdminPlatformMemberListResult(status, []);
    }
}

public sealed record AdminPlatformUserSetupResult(
    AdminPlatformRequestStatus Status,
    AdminPlatformUserSetupResponse? UserSetup)
{
    public static AdminPlatformUserSetupResult Success(AdminPlatformUserSetupResponse userSetup)
    {
        return new AdminPlatformUserSetupResult(AdminPlatformRequestStatus.Success, userSetup);
    }

    public static AdminPlatformUserSetupResult Failure(AdminPlatformRequestStatus status = AdminPlatformRequestStatus.Failure)
    {
        return new AdminPlatformUserSetupResult(status, null);
    }
}

public sealed record AdminPlatformUserSearchResult(
    AdminPlatformRequestStatus Status,
    IReadOnlyCollection<AdminPlatformUserSearchResponse> Users)
{
    public static AdminPlatformUserSearchResult Success(IReadOnlyCollection<AdminPlatformUserSearchResponse> users)
    {
        return new AdminPlatformUserSearchResult(AdminPlatformRequestStatus.Success, users);
    }

    public static AdminPlatformUserSearchResult Failure(AdminPlatformRequestStatus status = AdminPlatformRequestStatus.Failure)
    {
        return new AdminPlatformUserSearchResult(status, []);
    }
}

public sealed record AdminPlatformMembershipMutationResult(
    AdminPlatformRequestStatus Status,
    AdminPlatformMembershipResponse? Membership)
{
    public static AdminPlatformMembershipMutationResult Success(AdminPlatformMembershipResponse membership)
    {
        return new AdminPlatformMembershipMutationResult(AdminPlatformRequestStatus.Success, membership);
    }

    public static AdminPlatformMembershipMutationResult Failure(AdminPlatformRequestStatus status = AdminPlatformRequestStatus.Failure)
    {
        return new AdminPlatformMembershipMutationResult(status, null);
    }
}
