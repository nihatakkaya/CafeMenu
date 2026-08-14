using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace CafeMenu.Web.AdminAuth;

public sealed class RedisAdminSessionTokenStore : IAdminSessionTokenStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly AdminSessionOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new(StringComparer.Ordinal);

    public RedisAdminSessionTokenStore(
        IDistributedCache cache,
        IOptions<AdminSessionOptions> options,
        TimeProvider timeProvider)
    {
        _cache = cache;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task StoreAsync(AdminSessionTokens tokens, CancellationToken cancellationToken)
    {
        var timeToLive = tokens.RefreshTokenExpiresAt - _timeProvider.GetUtcNow();
        if (timeToLive < TimeSpan.FromSeconds(_options.MinimumCacheTtlSeconds))
        {
            await RemoveAsync(tokens.SessionId, cancellationToken);
            return;
        }

        var json = JsonSerializer.Serialize(tokens, JsonOptions);
        await _cache.SetStringAsync(
            BuildKey(tokens.SessionId),
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeToLive
            },
            cancellationToken);
    }

    public async Task<AdminSessionTokens?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var key = BuildKey(sessionId);
        var json = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        AdminSessionTokens? tokens;
        try
        {
            tokens = JsonSerializer.Deserialize<AdminSessionTokens>(json, JsonOptions);
        }
        catch (JsonException)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            return null;
        }

        if (tokens is null ||
            !string.Equals(tokens.SessionId, sessionId, StringComparison.Ordinal) ||
            tokens.RefreshTokenExpiresAt <= _timeProvider.GetUtcNow())
        {
            await _cache.RemoveAsync(key, cancellationToken);
            return null;
        }

        return tokens;
    }

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(sessionId)
            ? Task.CompletedTask
            : _cache.RemoveAsync(BuildKey(sessionId), cancellationToken);
    }

    public async Task<AdminSessionTokens?> RefreshAsync(
        string sessionId,
        DateTimeOffset refreshIfExpiresBefore,
        Func<AdminSessionTokens, CancellationToken, Task<AdminSessionTokens?>> refreshOperation,
        CancellationToken cancellationToken)
    {
        var refreshLock = _refreshLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync(cancellationToken);

        try
        {
            var currentTokens = await GetAsync(sessionId, cancellationToken);
            if (currentTokens is null)
            {
                return null;
            }

            if (currentTokens.AccessTokenExpiresAt > refreshIfExpiresBefore)
            {
                return currentTokens;
            }

            var refreshedTokens = await refreshOperation(currentTokens, cancellationToken);
            if (refreshedTokens is null)
            {
                var latestTokens = await GetAsync(sessionId, cancellationToken);
                if (latestTokens is not null &&
                    !string.Equals(latestTokens.RefreshToken, currentTokens.RefreshToken, StringComparison.Ordinal))
                {
                    return latestTokens;
                }

                await RemoveAsync(sessionId, cancellationToken);
                return null;
            }

            await StoreAsync(refreshedTokens, cancellationToken);
            return refreshedTokens;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public void Dispose()
    {
        foreach (var refreshLock in _refreshLocks.Values)
        {
            refreshLock.Dispose();
        }
    }

    private string BuildKey(string sessionId)
    {
        var digest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sessionId));
        return string.Concat(_options.KeyPrefix, WebEncoders.Base64UrlEncode(digest));
    }
}
