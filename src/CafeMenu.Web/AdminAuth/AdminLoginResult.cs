using System.Security.Claims;

namespace CafeMenu.Web.AdminAuth;

public sealed record AdminLoginResult(bool Succeeded, ClaimsPrincipal? Principal)
{
    public static AdminLoginResult Success(ClaimsPrincipal principal)
    {
        return new AdminLoginResult(true, principal);
    }

    public static AdminLoginResult Failure()
    {
        return new AdminLoginResult(false, null);
    }
}
