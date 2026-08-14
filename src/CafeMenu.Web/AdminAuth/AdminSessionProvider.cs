namespace CafeMenu.Web.AdminAuth;

public static class AdminSessionProvider
{
    public const string Memory = "Memory";
    public const string Redis = "Redis";

    public static bool IsMemory(string? provider)
    {
        return string.Equals(provider, Memory, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRedis(string? provider)
    {
        return string.Equals(provider, Redis, StringComparison.OrdinalIgnoreCase);
    }
}
