using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AccountSetup;

public sealed class AccountSetupFormModel
{
    [Required(ErrorMessage = "Kurulum kodu zorunludur.")]
    [StringLength(256, MinimumLength = 32, ErrorMessage = "Kurulum kodu geçersiz görünüyor.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Yeni şifre geçersiz.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Şifre tekrarı geçersiz.")]
    [Compare(nameof(Password), ErrorMessage = "Şifreler aynı olmalıdır.")]
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
