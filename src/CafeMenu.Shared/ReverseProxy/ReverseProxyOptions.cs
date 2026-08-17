using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Shared.ReverseProxy;

public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    public bool Enabled { get; init; }

    [Range(1, int.MaxValue)]
    public int ForwardLimit { get; init; } = 1;

    public string[] KnownProxies { get; init; } = [];

    public string[] KnownIPNetworks { get; init; } = [];
}
