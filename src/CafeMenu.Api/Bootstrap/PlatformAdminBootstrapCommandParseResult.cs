namespace CafeMenu.Api.Bootstrap;

public sealed record PlatformAdminBootstrapCommandParseResult(
    bool IsSuccess,
    string? Email,
    string? ErrorMessage)
{
    public static PlatformAdminBootstrapCommandParseResult Success(string email)
    {
        return new PlatformAdminBootstrapCommandParseResult(true, email, null);
    }

    public static PlatformAdminBootstrapCommandParseResult Failure(string errorMessage)
    {
        return new PlatformAdminBootstrapCommandParseResult(false, null, errorMessage);
    }
}
