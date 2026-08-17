namespace CafeMenu.Web.AdminBranding;

public enum AdminBrandingRequestStatus
{
    Success,
    ValidationError,
    Failure
}

public sealed record AdminBrandingRequestResult(
    AdminBrandingRequestStatus Status,
    AdminBrandingResponse? Branding)
{
    public static AdminBrandingRequestResult Success(AdminBrandingResponse branding)
    {
        return new AdminBrandingRequestResult(AdminBrandingRequestStatus.Success, branding);
    }

    public static AdminBrandingRequestResult ValidationError()
    {
        return new AdminBrandingRequestResult(AdminBrandingRequestStatus.ValidationError, null);
    }

    public static AdminBrandingRequestResult Failure()
    {
        return new AdminBrandingRequestResult(AdminBrandingRequestStatus.Failure, null);
    }
}
