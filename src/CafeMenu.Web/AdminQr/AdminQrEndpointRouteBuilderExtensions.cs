using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Web.AdminQr;

public static class AdminQrEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAdminQrEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/admin/cafes/{cafeId:long}/qr/download/png", DownloadPngAsync)
            .RequireAuthorization();

        endpoints.MapGet("/admin/cafes/{cafeId:long}/qr/download/svg", DownloadSvgAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> DownloadPngAsync(
        [FromRoute] long cafeId,
        IAdminQrCodeService qrCodeService,
        CancellationToken cancellationToken)
    {
        var result = await qrCodeService.GetDownloadAsync(cafeId, AdminQrDownloadFormat.Png, cancellationToken);

        return ToFileResult(result);
    }

    private static async Task<IResult> DownloadSvgAsync(
        [FromRoute] long cafeId,
        IAdminQrCodeService qrCodeService,
        CancellationToken cancellationToken)
    {
        var result = await qrCodeService.GetDownloadAsync(cafeId, AdminQrDownloadFormat.Svg, cancellationToken);

        return ToFileResult(result);
    }

    private static IResult ToFileResult(AdminQrDownloadResult result)
    {
        return result.Status switch
        {
            AdminQrRequestStatus.Success when result.File is not null => Results.File(
                result.File.Content,
                result.File.ContentType,
                result.File.FileName),
            AdminQrRequestStatus.NotFound => Results.NotFound(),
            _ => Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
        };
    }
}
