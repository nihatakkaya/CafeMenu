using Microsoft.Extensions.Options;

namespace CafeMenu.Web.AdminQr;

public sealed class AdminQrUrlBuilder : IAdminQrUrlBuilder
{
    private readonly PublicMenuQrOptions _options;

    public AdminQrUrlBuilder(IOptions<PublicMenuQrOptions> options)
    {
        _options = options.Value;
    }

    public string BuildPublicMenuUrl(string slug)
    {
        var baseUrl = _options.BaseUrl.Trim().TrimEnd('/');
        var escapedSlug = Uri.EscapeDataString(slug.Trim());

        return $"{baseUrl}/c/{escapedSlug}";
    }
}
