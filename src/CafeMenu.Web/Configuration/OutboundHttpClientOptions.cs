using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.Configuration;

public sealed class OutboundHttpClientOptions
{
    public const string SectionName = "HttpClients";

    public const int MinimumTimeoutSeconds = 1;

    public const int MaximumTimeoutSeconds = 120;

    [Range(MinimumTimeoutSeconds, MaximumTimeoutSeconds)]
    public int DefaultTimeoutSeconds { get; init; } = 15;
}
