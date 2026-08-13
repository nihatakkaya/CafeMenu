using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Web.AdminAuth;
using Microsoft.Extensions.Options;

namespace CafeMenu.Web.AccountSetup;

public sealed class AccountSetupApiClient : IAccountSetupApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AccountSetupApiClient(IHttpClientFactory httpClientFactory, IOptions<AdminApiOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient(AccountSetupConstants.ApiClientName);
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
    }

    public async Task<AccountSetupResult> CompleteUserSetupAsync(
        AccountSetupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "PlatformUser/CompleteUserSetup",
                request,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return AccountSetupResult.Success();
            }

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => AccountSetupResult.Failure(AccountSetupStatus.ValidationError),
                HttpStatusCode.Unauthorized => AccountSetupResult.Failure(AccountSetupStatus.InvalidToken),
                _ => AccountSetupResult.Failure(AccountSetupStatus.Failure)
            };
        }
        catch (HttpRequestException)
        {
            return AccountSetupResult.Failure(AccountSetupStatus.Failure);
        }
        catch (JsonException)
        {
            return AccountSetupResult.Failure(AccountSetupStatus.Failure);
        }
    }
}
