using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminCookieAuthenticationEvents : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var sessionId = context.Principal?.FindFirst(AdminAuthenticationConstants.SessionIdClaim)?.Value;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            await RejectPrincipalAsync(context);
            return;
        }

        var tokenStore = context.HttpContext.RequestServices.GetRequiredService<IAdminSessionTokenStore>();
        var tokens = await tokenStore.GetAsync(sessionId, context.HttpContext.RequestAborted);
        if (tokens is null)
        {
            await RejectPrincipalAsync(context);
        }
    }

    private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AdminAuthenticationConstants.CookieScheme);
    }
}
