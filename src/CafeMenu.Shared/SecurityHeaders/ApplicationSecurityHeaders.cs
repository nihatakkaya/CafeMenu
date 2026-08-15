namespace CafeMenu.Shared.SecurityHeaders;

public static class ApplicationSecurityHeaders
{
    public const string XContentTypeOptionsHeaderName = "X-Content-Type-Options";
    public const string XContentTypeOptionsValue = "nosniff";

    public const string ReferrerPolicyHeaderName = "Referrer-Policy";
    public const string ReferrerPolicyValue = "strict-origin-when-cross-origin";

    public const string XFrameOptionsHeaderName = "X-Frame-Options";
    public const string XFrameOptionsValue = "SAMEORIGIN";

    public const string PermissionsPolicyHeaderName = "Permissions-Policy";
    public const string PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    public const string ContentSecurityPolicyHeaderName = "Content-Security-Policy";
    public const string ContentSecurityPolicyValue = "frame-ancestors 'self'";
}
