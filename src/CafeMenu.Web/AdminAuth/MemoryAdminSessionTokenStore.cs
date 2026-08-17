using System.Collections.Concurrent;

namespace CafeMenu.Web.AdminAuth;

/// <summary>
/// Process-local development token store. Use a distributed implementation for multi-instance production deployments.
/// </summary>
public sealed class MemoryAdminSessionTokenStore : IAdminSessionTokenStore, IDisposable
{
    private readonly ConcurrentDictionary<string, AdminSessionTokens> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public MemoryAdminSessionTokenStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task StoreAsync(AdminSessionTokens tokens, CancellationToken cancellationToken)
    {
        if (IsExpired(tokens))
        {
            _sessions.TryRemove(tokens.SessionId, out _);
            return Task.CompletedTask;
        }

        _sessions[tokens.SessionId] = tokens;
        return Task.CompletedTask;
    }

    public Task<AdminSessionTokens?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        _sessions.TryGetValue(sessionId, out var tokens);
        if (tokens is not null && IsExpired(tokens))
        {
            _sessions.TryRemove(sessionId, out _);
            tokens = null;
        }

        return Task.FromResult(tokens);
    }

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
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
            if (!_sessions.TryGetValue(sessionId, out var currentTokens) || IsExpired(currentTokens))
            {
                _sessions.TryRemove(sessionId, out _);
                return null;
            }

            if (currentTokens.AccessTokenExpiresAt > refreshIfExpiresBefore)
            {
                return currentTokens;
            }

            var refreshedTokens = await refreshOperation(currentTokens, cancellationToken);
            if (refreshedTokens is null)
            {
                _sessions.TryRemove(sessionId, out _);
                return null;
            }

            _sessions[sessionId] = refreshedTokens;
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

    private bool IsExpired(AdminSessionTokens tokens)
    {
        return tokens.RefreshTokenExpiresAt <= _timeProvider.GetUtcNow();
    }
}
