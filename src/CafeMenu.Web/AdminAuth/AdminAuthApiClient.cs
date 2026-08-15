using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminAuthApiClient : IAdminAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AdminAuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<AdminAuthResponse?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        return SendForAuthResponseAsync(
            "Authentication/Login",
            new { Email = email, Password = password },
            cancellationToken);
    }

    public Task<AdminAuthResponse?> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        return SendForAuthResponseAsync(
            "Authentication/RefreshToken",
            new { RefreshToken = refreshToken },
            cancellationToken);
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "Authentication/Logout",
                new { RefreshToken = refreshToken },
                JsonOptions,
                cancellationToken);

            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<AdminAuthResponse?> SendForAuthResponseAsync(
        string requestUri,
        object request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminAuthResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? apiResponse.Data
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
