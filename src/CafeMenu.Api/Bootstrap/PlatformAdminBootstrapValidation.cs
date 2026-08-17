using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.Bootstrap;

public static class PlatformAdminBootstrapValidation
{
    private const int MaxEmailLength = 320;
    private const int MinPasswordLength = 12;
    private const int MaxPasswordLength = 128;

    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    public static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            email.Length <= MaxEmailLength &&
            EmailAddressAttribute.IsValid(email);
    }

    public static IReadOnlyCollection<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < MinPasswordLength)
        {
            errors.Add($"Password must be at least {MinPasswordLength} characters.");
        }

        if (password.Length > MaxPasswordLength)
        {
            errors.Add($"Password must be at most {MaxPasswordLength} characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain an uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain a lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain a digit.");
        }

        if (!password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("Password must contain a symbol.");
        }

        return errors;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
