namespace CafeMenu.Web.AdminBranding;

public interface IAdminBrandingApiClient
{
    Task<AdminBrandingRequestResult> GetCafeBrandingAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminBrandingRequestResult> UpdateCafeBrandingAsync(
        long cafeId,
        AdminUpdateCafeBrandingRequest request,
        CancellationToken cancellationToken);
}
