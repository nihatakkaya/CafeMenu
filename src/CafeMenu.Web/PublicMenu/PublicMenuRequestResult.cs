namespace CafeMenu.Web.PublicMenu;

public enum PublicMenuRequestStatus
{
    Success,
    NotFound,
    Failure
}

public sealed record PublicMenuRequestResult(
    PublicMenuRequestStatus Status,
    PublicMenuResponse? Menu)
{
    public static PublicMenuRequestResult Success(PublicMenuResponse menu)
    {
        return new PublicMenuRequestResult(PublicMenuRequestStatus.Success, menu);
    }

    public static PublicMenuRequestResult NotFound()
    {
        return new PublicMenuRequestResult(PublicMenuRequestStatus.NotFound, null);
    }

    public static PublicMenuRequestResult Failure()
    {
        return new PublicMenuRequestResult(PublicMenuRequestStatus.Failure, null);
    }
}

public sealed record PublicProductDetailRequestResult(
    PublicMenuRequestStatus Status,
    PublicProductDetailResponse? Product)
{
    public static PublicProductDetailRequestResult Success(PublicProductDetailResponse product)
    {
        return new PublicProductDetailRequestResult(PublicMenuRequestStatus.Success, product);
    }

    public static PublicProductDetailRequestResult NotFound()
    {
        return new PublicProductDetailRequestResult(PublicMenuRequestStatus.NotFound, null);
    }

    public static PublicProductDetailRequestResult Failure()
    {
        return new PublicProductDetailRequestResult(PublicMenuRequestStatus.Failure, null);
    }
}
