using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Web.AdminImageUpload;

public static class AdminImageUploadEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAdminImageUploadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/admin/cafes/{cafeId:long}/branding/upload-logo", UploadCafeLogoAsync).RequireAuthorization();
        endpoints.MapPost("/admin/cafes/{cafeId:long}/branding/upload-cover", UploadCafeCoverAsync).RequireAuthorization();
        endpoints.MapPost("/admin/cafes/{cafeId:long}/branding/remove-logo", RemoveCafeLogoAsync).RequireAuthorization();
        endpoints.MapPost("/admin/cafes/{cafeId:long}/branding/remove-cover", RemoveCafeCoverAsync).RequireAuthorization();

        endpoints.MapPost("/admin/cafes/{cafeId:long}/categories/{categoryId:long}/upload-image", UploadCategoryImageAsync).RequireAuthorization();
        endpoints.MapPost("/admin/cafes/{cafeId:long}/categories/{categoryId:long}/remove-image", RemoveCategoryImageAsync).RequireAuthorization();

        endpoints.MapPost("/admin/cafes/{cafeId:long}/products/{productId:long}/upload-image", UploadProductImageAsync).RequireAuthorization();
        endpoints.MapPost("/admin/cafes/{cafeId:long}/products/{productId:long}/remove-image", RemoveProductImageAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> UploadCafeLogoAsync(
        [FromRoute] long cafeId,
        [FromForm] AdminImageUploadForm form,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await UploadIfPresentAsync(form, file => imageUploadApiClient.UploadCafeLogoAsync(cafeId, file, cancellationToken));
        return RedirectToBranding(cafeId, status);
    }

    private static async Task<IResult> UploadCafeCoverAsync(
        [FromRoute] long cafeId,
        [FromForm] AdminImageUploadForm form,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await UploadIfPresentAsync(form, file => imageUploadApiClient.UploadCafeCoverAsync(cafeId, file, cancellationToken));
        return RedirectToBranding(cafeId, status);
    }

    private static async Task<IResult> RemoveCafeLogoAsync(
        [FromRoute] long cafeId,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await imageUploadApiClient.RemoveCafeLogoAsync(cafeId, cancellationToken);
        return RedirectToBranding(cafeId, status);
    }

    private static async Task<IResult> RemoveCafeCoverAsync(
        [FromRoute] long cafeId,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await imageUploadApiClient.RemoveCafeCoverAsync(cafeId, cancellationToken);
        return RedirectToBranding(cafeId, status);
    }

    private static async Task<IResult> UploadCategoryImageAsync(
        [FromRoute] long cafeId,
        [FromRoute] long categoryId,
        [FromForm] AdminImageUploadForm form,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await UploadIfPresentAsync(form, file => imageUploadApiClient.UploadCategoryImageAsync(categoryId, file, cancellationToken));
        return RedirectToCategories(cafeId, status);
    }

    private static async Task<IResult> RemoveCategoryImageAsync(
        [FromRoute] long cafeId,
        [FromRoute] long categoryId,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await imageUploadApiClient.RemoveCategoryImageAsync(categoryId, cancellationToken);
        return RedirectToCategories(cafeId, status);
    }

    private static async Task<IResult> UploadProductImageAsync(
        [FromRoute] long cafeId,
        [FromRoute] long productId,
        [FromForm] AdminImageUploadForm form,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await UploadIfPresentAsync(form, file => imageUploadApiClient.UploadProductImageAsync(productId, file, cancellationToken));
        return RedirectToProducts(cafeId, status);
    }

    private static async Task<IResult> RemoveProductImageAsync(
        [FromRoute] long cafeId,
        [FromRoute] long productId,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminImageUploadApiClient imageUploadApiClient,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var status = await imageUploadApiClient.RemoveProductImageAsync(productId, cancellationToken);
        return RedirectToProducts(cafeId, status);
    }

    private static Task<AdminImageUploadStatus> UploadIfPresentAsync(
        AdminImageUploadForm form,
        Func<IFormFile, Task<AdminImageUploadStatus>> upload)
    {
        return form.File is null || form.File.Length == 0
            ? Task.FromResult(AdminImageUploadStatus.ValidationError)
            : upload(form.File);
    }

    private static IResult RedirectToBranding(long cafeId, AdminImageUploadStatus status)
    {
        return Results.Redirect($"/admin/cafes/{cafeId}/branding?imageUpload={ToQueryValue(status)}");
    }

    private static IResult RedirectToCategories(long cafeId, AdminImageUploadStatus status)
    {
        return Results.Redirect($"/admin/cafes/{cafeId}/categories?imageUpload={ToQueryValue(status)}");
    }

    private static IResult RedirectToProducts(long cafeId, AdminImageUploadStatus status)
    {
        return Results.Redirect($"/admin/cafes/{cafeId}/products?imageUpload={ToQueryValue(status)}");
    }

    private static string ToQueryValue(AdminImageUploadStatus status)
    {
        return status switch
        {
            AdminImageUploadStatus.Success => "success",
            AdminImageUploadStatus.ValidationError => "validation",
            _ => "failure"
        };
    }
}
