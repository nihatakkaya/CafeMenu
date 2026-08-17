namespace CafeMenu.Web.AdminAuth;

public sealed class AdminApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }
}
