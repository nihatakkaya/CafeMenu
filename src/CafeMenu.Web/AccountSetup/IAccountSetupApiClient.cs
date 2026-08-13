namespace CafeMenu.Web.AccountSetup;

public interface IAccountSetupApiClient
{
    Task<AccountSetupResult> CompleteUserSetupAsync(
        AccountSetupRequest request,
        CancellationToken cancellationToken);
}
