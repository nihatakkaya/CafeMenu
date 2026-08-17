using CafeMenu.Web.AdminCafe;

namespace CafeMenu.Web.AdminQr;

public sealed class AdminQrCodeService : IAdminQrCodeService
{
    private const string PngContentType = "image/png";
    private const string SvgContentType = "image/svg+xml";

    private readonly IAdminCafeApiClient _adminCafeApiClient;
    private readonly IAdminQrUrlBuilder _urlBuilder;
    private readonly IAdminQrCodeRenderer _renderer;

    public AdminQrCodeService(
        IAdminCafeApiClient adminCafeApiClient,
        IAdminQrUrlBuilder urlBuilder,
        IAdminQrCodeRenderer renderer)
    {
        _adminCafeApiClient = adminCafeApiClient;
        _urlBuilder = urlBuilder;
        _renderer = renderer;
    }

    public async Task<AdminQrPageResult> GetPageModelAsync(long cafeId, CancellationToken cancellationToken)
    {
        var cafeResult = await GetAccessibleCafeAsync(cafeId, cancellationToken);
        if (cafeResult.Status != AdminQrRequestStatus.Success || cafeResult.Cafe is null)
        {
            return AdminQrPageResult.Failure(cafeResult.Status);
        }

        var publicMenuUrl = _urlBuilder.BuildPublicMenuUrl(cafeResult.Cafe.Slug);
        var png = _renderer.GeneratePng(publicMenuUrl);
        var model = new AdminQrPageModel(
            cafeResult.Cafe,
            publicMenuUrl,
            $"data:image/png;base64,{Convert.ToBase64String(png)}",
            $"/admin/cafes/{cafeResult.Cafe.Id}/qr/download/png",
            $"/admin/cafes/{cafeResult.Cafe.Id}/qr/download/svg");

        return AdminQrPageResult.Success(model);
    }

    public async Task<AdminQrDownloadResult> GetDownloadAsync(
        long cafeId,
        AdminQrDownloadFormat format,
        CancellationToken cancellationToken)
    {
        var cafeResult = await GetAccessibleCafeAsync(cafeId, cancellationToken);
        if (cafeResult.Status != AdminQrRequestStatus.Success || cafeResult.Cafe is null)
        {
            return AdminQrDownloadResult.Failure(cafeResult.Status);
        }

        var publicMenuUrl = _urlBuilder.BuildPublicMenuUrl(cafeResult.Cafe.Slug);
        var fileNameBase = BuildSafeFileNameBase(cafeResult.Cafe.Slug);
        var file = format == AdminQrDownloadFormat.Png
            ? new AdminQrDownloadFile(
                _renderer.GeneratePng(publicMenuUrl),
                PngContentType,
                $"{fileNameBase}.png",
                publicMenuUrl)
            : new AdminQrDownloadFile(
                _renderer.GenerateSvgBytes(publicMenuUrl),
                SvgContentType,
                $"{fileNameBase}.svg",
                publicMenuUrl);

        return AdminQrDownloadResult.Success(file);
    }

    private async Task<AccessibleCafeResult> GetAccessibleCafeAsync(long cafeId, CancellationToken cancellationToken)
    {
        var cafeResult = await _adminCafeApiClient.GetMyCafesAsync(cancellationToken);
        if (cafeResult.Status != AdminCafeListStatus.Success)
        {
            return AccessibleCafeResult.Failure(AdminQrRequestStatus.Failure);
        }

        var cafe = cafeResult.Cafes.SingleOrDefault(existingCafe => existingCafe.Id == cafeId);

        return cafe is null
            ? AccessibleCafeResult.Failure(AdminQrRequestStatus.NotFound)
            : AccessibleCafeResult.Success(cafe);
    }

    private static string BuildSafeFileNameBase(string slug)
    {
        var safeSlug = new string(slug
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) || character == '-'
                ? character
                : '-')
            .ToArray());

        safeSlug = string.Join('-', safeSlug.Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(safeSlug)
            ? "cafe-menu-qr"
            : $"{safeSlug}-menu-qr";
    }

    private sealed record AccessibleCafeResult(AdminQrRequestStatus Status, AdminCafeResponse? Cafe)
    {
        public static AccessibleCafeResult Success(AdminCafeResponse cafe)
        {
            return new AccessibleCafeResult(AdminQrRequestStatus.Success, cafe);
        }

        public static AccessibleCafeResult Failure(AdminQrRequestStatus status)
        {
            return new AccessibleCafeResult(status, null);
        }
    }
}
