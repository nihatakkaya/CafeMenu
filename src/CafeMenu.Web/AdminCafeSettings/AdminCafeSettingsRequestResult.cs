namespace CafeMenu.Web.AdminCafeSettings;

public enum AdminCafeSettingsRequestStatus
{
    Success,
    ValidationError,
    Failure
}

public sealed record AdminCafeSettingsRequestResult(
    AdminCafeSettingsRequestStatus Status,
    AdminCafeSettingsResponse? Settings)
{
    public static AdminCafeSettingsRequestResult Success(AdminCafeSettingsResponse settings)
    {
        return new AdminCafeSettingsRequestResult(AdminCafeSettingsRequestStatus.Success, settings);
    }

    public static AdminCafeSettingsRequestResult ValidationError()
    {
        return new AdminCafeSettingsRequestResult(AdminCafeSettingsRequestStatus.ValidationError, null);
    }

    public static AdminCafeSettingsRequestResult Failure()
    {
        return new AdminCafeSettingsRequestResult(AdminCafeSettingsRequestStatus.Failure, null);
    }
}
