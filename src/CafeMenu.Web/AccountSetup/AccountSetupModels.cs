using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AccountSetup;

public sealed class AccountSetupFormModel
{
    [Required(ErrorMessage = "Setup kodu zorunludur.")]
    [StringLength(256, MinimumLength = 32, ErrorMessage = "Setup kodu gecersiz gorunuyor.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni sifre zorunludur.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Yeni sifre gecersiz.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre tekrari zorunludur.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Sifre tekrari gecersiz.")]
    [Compare(nameof(Password), ErrorMessage = "Sifreler ayni olmalidir.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed record AccountSetupRequest(
    string Token,
    string Password,
    string ConfirmPassword);

public sealed record AccountSetupResult(AccountSetupStatus Status)
{
    public static AccountSetupResult Success()
    {
        return new AccountSetupResult(AccountSetupStatus.Success);
    }

    public static AccountSetupResult Failure(AccountSetupStatus status)
    {
        return new AccountSetupResult(status);
    }
}

public enum AccountSetupStatus
{
    Success,
    ValidationError,
    InvalidToken,
    Failure
}
