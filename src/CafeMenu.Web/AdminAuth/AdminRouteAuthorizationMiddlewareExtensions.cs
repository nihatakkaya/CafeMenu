namespace CafeMenu.Web.AdminAuth;

public static class AdminRouteAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminRouteAuthorizationRedirect(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase) &&
                context.User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = string.Concat(
                    context.Request.PathBase.ToString(),
                    context.Request.Path.ToString(),
                    context.Request.QueryString.ToString());

                context.Response.Redirect($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }

            await next(context);
        });
    }
}
