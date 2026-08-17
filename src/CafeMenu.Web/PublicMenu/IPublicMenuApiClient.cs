namespace CafeMenu.Web.PublicMenu;

public interface IPublicMenuApiClient
{
    Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken);

    Task<PublicProductDetailRequestResult> GetProductDetailAsync(
        string slug,
        long productId,
        CancellationToken cancellationToken);
}
