namespace CafeMenu.Api.Configuration;

public sealed class DatabaseRetryOptions
{
    public bool Enabled { get; set; } = true;

    public int MaxRetryCount { get; set; } = 3;

    public int MaxRetryDelaySeconds { get; set; } = 5;
}
