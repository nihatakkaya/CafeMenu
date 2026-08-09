namespace CafeMenu.Api.Bootstrap;

public sealed record PlatformAdminBootstrapResult(
    PlatformAdminBootstrapStatus Status,
    long? UserId,
    string Email,
    string Message)
{
    public static PlatformAdminBootstrapResult Created(long userId, string email)
    {
        return new PlatformAdminBootstrapResult(
            PlatformAdminBootstrapStatus.Created,
            userId,
            email,
            "Platform admin user created.");
    }

    public static PlatformAdminBootstrapResult AlreadyExists(long userId, string email)
    {
        return new PlatformAdminBootstrapResult(
            PlatformAdminBootstrapStatus.AlreadyExists,
            userId,
            email,
            "User already exists. No changes were made.");
    }

    public static PlatformAdminBootstrapResult Failure(
        PlatformAdminBootstrapStatus status,
        string email,
        string message)
    {
        return new PlatformAdminBootstrapResult(status, null, email, message);
    }
}
