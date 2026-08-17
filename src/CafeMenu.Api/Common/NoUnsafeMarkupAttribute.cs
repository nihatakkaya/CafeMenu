using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NoUnsafeMarkupAttribute : ValidationAttribute
{
    private static readonly string[] UnsafeTokens =
    [
        "<",
        ">",
        "{",
        "}",
        ";",
        "<script",
        "</script",
        "<style",
        "</style",
        "javascript:",
        "expression("
    ];

    public NoUnsafeMarkupAttribute()
    {
        ErrorMessage = "The value contains unsupported markup, style or script content.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not string text)
        {
            return false;
        }

        return !UnsafeTokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
