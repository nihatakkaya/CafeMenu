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
            ? "<p class=\"validation-message\" role=\"alert\">E-posta veya şifre hatalı.</p>"
            : string.Empty;
        var setupSuccessHtml = string.Equals(setup, "success", StringComparison.Ordinal)
            ? "<p class=\"validation-message\" role=\"status\">Hesabınız hazır. Şimdi giriş yapabilirsiniz.</p>"
            : string.Empty;

        return $$"""
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Yönetici Girişi</title>
    <style>
        :root {
            color-scheme: light;
            --cm-bg: #f5f7f9;
            --cm-surface: #ffffff;
            --cm-text: #142033;
            --cm-muted: #5b6676;
            --cm-border: #d8e0ea;
            --cm-primary: #18324a;
            --cm-danger-bg: #fff3f3;
            --cm-danger-text: #8f1d1d;
            --cm-success-bg: #e9f7ef;
            --cm-success-text: #12653a;
        }

        * {
            box-sizing: border-box;
        }

        html,
        body {
            min-height: 100%;
            margin: 0;
        }

        body {
            background: var(--cm-bg);
            color: var(--cm-text);
            font-family: ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            letter-spacing: 0;
        }

        .account-page {
            align-items: center;
            display: grid;
            min-height: 100vh;
            padding: 1rem;
        }

        .account-panel {
            background: var(--cm-surface);
            border: 1px solid var(--cm-border);
            border-radius: 8px;
            box-shadow: 0 16px 44px rgba(20, 32, 51, 0.08);
            display: grid;
            gap: 1rem;
            margin: 0 auto;
            max-width: 28rem;
            padding: 1.35rem;
            width: 100%;
        }

        .account-kicker {
            color: var(--cm-muted);
            font-size: 0.78rem;
            font-weight: 850;
            margin: 0;
            text-transform: uppercase;
        }

        h1 {
            font-size: 1.85rem;
            line-height: 1.1;
            margin: 0;
        }

        .account-summary {
            color: var(--cm-muted);
            line-height: 1.5;
            margin: 0;
        }

        form,
        label {
            display: grid;
            gap: 0.45rem;
        }

        form {
            gap: 0.85rem;
        }

        label {
            color: #25364a;
            font-weight: 750;
        }

        input {
            border: 1px solid #bcc9d8;
            border-radius: 6px;
            color: var(--cm-text);
            font: inherit;
            min-height: 44px;
            padding: 0.65rem 0.75rem;
            width: 100%;
        }

        input:focus-visible,
        button:focus-visible {
            outline: 0;
            box-shadow: 0 0 0 3px rgba(36, 71, 97, 0.2);
        }

        button {
            background: var(--cm-primary);
            border: 1px solid var(--cm-primary);
            border-radius: 6px;
            color: #ffffff;
            cursor: pointer;
            font: inherit;
            font-weight: 850;
            min-height: 44px;
            padding: 0.65rem 0.85rem;
        }

        .validation-message {
            border-radius: 6px;
            font-weight: 750;
            margin: 0;
            padding: 0.7rem 0.8rem;
        }

        .validation-message[role="alert"] {
            background: var(--cm-danger-bg);
            color: var(--cm-danger-text);
        }

        .validation-message[role="status"] {
            background: var(--cm-success-bg);
            color: var(--cm-success-text);
        }
    </style>
</head>
<body>
    <main class="account-page">
        <section class="account-panel">
            <p class="account-kicker">CafeMenu Yönetimi</p>
            <h1>Yönetici Girişi</h1>
            <p class="account-summary">Yönetim hesabınızla güvenli şekilde giriş yapın.</p>
            {{errorHtml}}
            {{setupSuccessHtml}}
            <form method="post" action="/account/login">
                <input name="{{encodedFieldName}}" type="hidden" value="{{encodedToken}}" />
                <input name="ReturnUrl" type="hidden" value="{{encodedReturnUrl}}" />
                <label>
                    E-posta
                    <input name="Email" type="email" autocomplete="username" required maxlength="320" />
                </label>
                <label>
                    Şifre
                    <input name="Password" type="password" autocomplete="current-password" required maxlength="128" />
                </label>
                <button type="submit">Giriş Yap</button>
            </form>
        </section>
    </main>
</body>
</html>
""";
    }
}
