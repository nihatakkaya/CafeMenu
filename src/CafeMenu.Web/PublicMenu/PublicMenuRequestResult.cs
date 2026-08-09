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
