namespace CafeMenu.Web.AdminCafe;

public sealed class AdminCafeResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? LogoImageUrl { get; init; }

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public IReadOnlyCollection<string> RoleCodes { get; init; } = [];
}
