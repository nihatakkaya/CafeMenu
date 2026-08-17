namespace CafeMenu.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseRetryOptions Retry { get; set; } = new();
}
