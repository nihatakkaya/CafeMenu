using System.ComponentModel.DataAnnotations;
using CafeMenu.Web.AdminCafe;

namespace CafeMenu.Web.AdminQr;

public sealed class PublicMenuQrOptions
{
    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}

public sealed record AdminQrPageModel(
    AdminCafeResponse Cafe,
    string PublicMenuUrl,
    string PreviewPngDataUri,
    string PngDownloadUrl,
    string SvgDownloadUrl);

public sealed record AdminQrDownloadFile(
    byte[] Content,
    string ContentType,
    string FileName,
    string EncodedUrl);

public sealed record AdminQrDownloadResult(
    AdminQrRequestStatus Status,
    AdminQrDownloadFile? File)
{
    public static AdminQrDownloadResult Success(AdminQrDownloadFile file)
    {
        return new AdminQrDownloadResult(AdminQrRequestStatus.Success, file);
    }

    public static AdminQrDownloadResult Failure(AdminQrRequestStatus status = AdminQrRequestStatus.Failure)
    {
        return new AdminQrDownloadResult(status, null);
    }
}

public sealed record AdminQrPageResult(
    AdminQrRequestStatus Status,
    AdminQrPageModel? QrCode)
{
    public static AdminQrPageResult Success(AdminQrPageModel qrCode)
    {
        return new AdminQrPageResult(AdminQrRequestStatus.Success, qrCode);
    }

    public static AdminQrPageResult Failure(AdminQrRequestStatus status = AdminQrRequestStatus.Failure)
    {
        return new AdminQrPageResult(status, null);
    }
}

public enum AdminQrRequestStatus
{
    Success,
    NotFound,
    Failure
}

public enum AdminQrDownloadFormat
{
    Png,
    Svg
}
