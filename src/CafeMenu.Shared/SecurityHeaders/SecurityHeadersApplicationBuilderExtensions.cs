using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CafeMenu.Shared.SecurityHeaders;

public static class SecurityHeadersApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApplicationSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(static state =>
            {
                var httpContext = (HttpContext)state;
                var headers = httpContext.Response.Headers;

                SetIfAbsent(
                    headers,
                    ApplicationSecurityHeaders.XContentTypeOptionsHeaderName,
                    ApplicationSecurityHeaders.XContentTypeOptionsValue);
                SetIfAbsent(
                    headers,
                    ApplicationSecurityHeaders.ReferrerPolicyHeaderName,
                    ApplicationSecurityHeaders.ReferrerPolicyValue);
                SetIfAbsent(
                    headers,
                    ApplicationSecurityHeaders.XFrameOptionsHeaderName,
                    ApplicationSecurityHeaders.XFrameOptionsValue);
                SetIfAbsent(
                    headers,
                    ApplicationSecurityHeaders.PermissionsPolicyHeaderName,
                    ApplicationSecurityHeaders.PermissionsPolicyValue);
                SetIfAbsent(
                    headers,
                    ApplicationSecurityHeaders.ContentSecurityPolicyHeaderName,
                    ApplicationSecurityHeaders.ContentSecurityPolicyValue);

                return Task.CompletedTask;
            }, context);

            await next();
        });
    }

    private static void SetIfAbsent(IHeaderDictionary headers, string headerName, string headerValue)
    {
        if (!headers.ContainsKey(headerName))
        {
            headers[headerName] = headerValue;
        }
    }
}
