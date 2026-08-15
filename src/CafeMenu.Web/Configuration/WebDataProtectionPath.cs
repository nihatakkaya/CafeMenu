namespace CafeMenu.Web.Configuration;

internal static class WebDataProtectionPath
{
    public static bool TryNormalizeAbsolutePath(string? path, out string normalizedPath)
    {
        normalizedPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var trimmedPath = path.Trim();
            if (!Path.IsPathFullyQualified(trimmedPath))
            {
                return false;
            }

            normalizedPath = Path.GetFullPath(trimmedPath);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
