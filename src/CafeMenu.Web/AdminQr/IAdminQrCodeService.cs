namespace CafeMenu.Web.AdminQr;

public interface IAdminQrCodeService
{
    Task<AdminQrPageResult> GetPageModelAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminQrDownloadResult> GetDownloadAsync(
        long cafeId,
        AdminQrDownloadFormat format,
        CancellationToken cancellationToken);
}
