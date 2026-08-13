using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Web.AdminAuth;

public static class AdminAuthEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAdminAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/account/login", LoginPage);
        endpoints.MapPost("/account/login", LoginAsync);

        endpoints.MapPost("/account/logout", LogoutAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static IResult LoginPage(HttpContext httpContext, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        var error = httpContext.Request.Query["error"].ToString();
        var setup = httpContext.Request.Query["setup"].ToString();
        var returnUrl = GetSafeReturnUrl(httpContext.Request.Query["returnUrl"].ToString());
        var html = BuildLoginPageHtml(tokens.FormFieldName, tokens.RequestToken ?? string.Empty, error, setup, returnUrl);

        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> LoginAsync(
        [FromForm] AdminLoginForm form,
        HttpContext httpContext,
        IAdminAuthService adminAuthService,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        if (!IsValid(form))
        {
            return Results.Redirect(BuildLoginFailureUrl(form.ReturnUrl));
        }

        var loginResult = await adminAuthService.LoginAsync(
            new AdminLoginCommand(form.Email, form.Password),
            cancellationToken);

        if (!loginResult.Succeeded || loginResult.Principal is null)
        {
            return Results.Redirect(BuildLoginFailureUrl(form.ReturnUrl));
        }

        await httpContext.SignInAsync(
            AdminAuthenticationConstants.CookieScheme,
            loginResult.Principal);

        return Results.Redirect(GetSafeReturnUrl(form.ReturnUrl));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IAdminAuthService adminAuthService,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        await adminAuthService.LogoutAsync(httpContext.User, cancellationToken);
        await httpContext.SignOutAsync(AdminAuthenticationConstants.CookieScheme);

        return Results.Redirect("/account/login");
    }

    private static bool IsValid(AdminLoginForm form)
    {
        var validationContext = new ValidationContext(form);
        return Validator.TryValidateObject(form, validationContext, validationResults: null, validateAllProperties: true);
    }

    private static string BuildLoginFailureUrl(string? returnUrl)
    {
        var url = "/account/login?error=invalid";
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        return safeReturnUrl == "/"
            ? url
            : $"{url}&returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
    }

    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return Uri.TryCreate(returnUrl, UriKind.Relative, out _) &&
            returnUrl.StartsWith("/", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
                ? returnUrl
                : "/";
    }

    private static string BuildLoginPageHtml(
        string formFieldName,
        string requestToken,
        string? error,
        string? setup,
        string returnUrl)
    {
        var encodedFieldName = WebUtility.HtmlEncode(formFieldName);
        var encodedToken = WebUtility.HtmlEncode(requestToken);
        var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl);
        var errorHtml = string.Equals(error, "invalid", StringComparison.Ordinal)
            ? "<p class=\"validation-message\" role=\"alert\">Email or password is invalid.</p>"
            : string.Empty;
        var setupSuccessHtml = string.Equals(setup, "success", StringComparison.Ordinal)
            ? "<p class=\"validation-message\" role=\"status\">Hesabiniz hazir. Simdi giris yapabilirsiniz.</p>"
            : string.Empty;

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Admin Login</title>
</head>
<body>
    <main class="account-page">
        <section class="account-panel">
            <h1>Admin Login</h1>
            {{errorHtml}}
            {{setupSuccessHtml}}
            <form method="post" action="/account/login">
                <input name="{{encodedFieldName}}" type="hidden" value="{{encodedToken}}" />
                <input name="ReturnUrl" type="hidden" value="{{encodedReturnUrl}}" />
                <label>
                    Email
                    <input name="Email" type="email" autocomplete="username" required maxlength="320" />
                </label>
                <label>
                    Password
                    <input name="Password" type="password" autocomplete="current-password" required maxlength="128" />
                </label>
                <button type="submit">Login</button>
            </form>
        </section>
    </main>
</body>
</html>
""";
    }
}
