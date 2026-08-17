using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.ReverseProxy;

public sealed class ReverseProxyForwardedHeadersOptionsSetup : IConfigureOptions<ForwardedHeadersOptions>
{
    private readonly IOptions<ReverseProxyOptions> _reverseProxyOptions;

    public ReverseProxyForwardedHeadersOptionsSetup(IOptions<ReverseProxyOptions> reverseProxyOptions)
    {
        _reverseProxyOptions = reverseProxyOptions;
    }

    public void Configure(ForwardedHeadersOptions options)
    {
        var reverseProxyOptions = _reverseProxyOptions.Value;

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = reverseProxyOptions.ForwardLimit;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var knownProxy in reverseProxyOptions.KnownProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(knownProxy));
        }

        foreach (var knownIPNetwork in reverseProxyOptions.KnownIPNetworks)
        {
            if (!ReverseProxyCidrParser.TryParse(knownIPNetwork, out var network))
            {
                continue;
            }

            options.KnownIPNetworks.Add(network);
        }
    }
}
