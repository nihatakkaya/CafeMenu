namespace CafeMenu.Web.AdminCafeSettings;

public interface IAdminCafeSettingsApiClient
{
    Task<AdminCafeSettingsRequestResult> GetCafeSettingsAsync(
        long cafeId,
        CancellationToken cancellationToken);

    Task<AdminCafeSettingsRequestResult> UpdateCafeSettingsAsync(
        long cafeId,
        AdminUpdateCafeSettingsRequest request,
        CancellationToken cancellationToken);

    Task<AdminCafeSettingsRequestResult> ChangeCafePublicationAsync(
        long cafeId,
        AdminChangeCafePublicationRequest request,
        CancellationToken cancellationToken);
}
