namespace CafeMenu.Shared.ReverseProxy;

internal static class ReverseProxyCidrParser
{
    public static bool TryParse(string? value, out System.Net.IPNetwork network)
    {
        network = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return System.Net.IPNetwork.TryParse(value, out network);
    }
}
