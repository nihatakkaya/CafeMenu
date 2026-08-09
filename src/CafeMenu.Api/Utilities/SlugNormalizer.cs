namespace CafeMenu.Api.Utilities;

public static class SlugNormalizer
{
    public static string Normalize(string value)
    {
        var chars = new List<char>(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (character is >= 'a' and <= 'z' || character is >= '0' and <= '9')
            {
                chars.Add(character);
                previousWasSeparator = false;
                continue;
            }

            if ((char.IsWhiteSpace(character) || character == '-') && !previousWasSeparator && chars.Count > 0)
            {
                chars.Add('-');
                previousWasSeparator = true;
            }
        }

        while (chars.Count > 0 && chars[^1] == '-')
        {
            chars.RemoveAt(chars.Count - 1);
        }

        return new string(chars.ToArray());
    }
}
