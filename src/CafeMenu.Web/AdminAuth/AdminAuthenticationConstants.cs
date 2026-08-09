using System.Security.Claims;

namespace CafeMenu.Web.AdminAuth;

public static class AdminAuthenticationConstants
{
    public const string CookieScheme = "CafeMenuAdmin";
    public const string SessionIdClaim = "web_session_id";
    public const string AppUserIdClaim = "app_user_id";
    public const string AdminApiClientName = "CafeMenuAdminApi";
    public const string AdminAuthApiClientName = "CafeMenuAdminAuthApi";

    public static readonly IReadOnlyCollection<string> IdentityClaimTypes =
    [
        AppUserIdClaim,
        ClaimTypes.NameIdentifier,
        ClaimTypes.Email,
        ClaimTypes.Name,
        ClaimTypes.Role,
        SessionIdClaim
    ];
}
