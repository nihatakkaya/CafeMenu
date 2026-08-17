namespace CafeMenu.Web.AdminAuth;

public sealed record AdminUserResponse(
    long Id,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles);
