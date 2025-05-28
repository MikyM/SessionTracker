using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Remora.Results;
using SessionTracker.Abstractions;

namespace SessionTracker.InMemory;

/// <summary>
/// In-memory implementation of <see cref="ISessionLockProvider"/>.
/// </summary>
[PublicAPI]
public class InMemorySessionLockProvider : ISessionLockProvider
{
    private static string CreateLockId()
        => Guid.NewGuid().ToString();

    private readonly MemoryCacheQueue _cacheQueue;
    private readonly InMemorySessionTrackerKeyCreator _keyCreator;
    private readonly ILogger<InMemorySessionLockProvider> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new instance of <see cref="InMemorySessionLockProvider"/>.
    /// </summary>
    /// <param name="cacheQueue">The cache queue.</param>
    /// <param name="keyCreator">Key creator.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="timeProvider">Time provider.</param>
    public InMemorySessionLockProvider(MemoryCacheQueue cacheQueue, InMemorySessionTrackerKeyCreator keyCreator, 
        ILogger<InMemorySessionLockProvider> logger, TimeProvider timeProvider)
    {
        _cacheQueue = cacheQueue;
        _keyCreator = keyCreator;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    private async Task<InMemorySessionLock> AcquirePrivateAsync<TSession>(string resource, TimeSpan lockExpirationTime)
        where TSession : Session
    {
        var lockId = CreateLockId();

        var lockKey = _keyCreator.CreateLockKey<TSession>(resource);

        _logger.LogDebug(
            "Attempting to acquire lock for {Resource} with {Exp} expiration time, assigned ID {Id}, assigned lock key {Key}",
            resource, lockExpirationTime, lockId, lockKey);

        var acquireResult = await _cacheQueue.EnqueueAsync(x =>
        {
            var exists = x.TryGetValue(lockKey, out _);

            if (exists)
            {
                return new InMemorySessionLock(lockKey, lockId, false, SessionLockStatus.Conflicted, null,
                    _timeProvider.GetUtcNow().Add(lockExpirationTime));
            }

            var lc = new InMemorySessionLock(lockKey, lockId, true, SessionLockStatus.Acquired, _cacheQueue,
                _timeProvider.GetUtcNow().Add(lockExpirationTime));

            var expToken =
                new CancellationChangeToken(
                    new CancellationTokenSource(lockExpirationTime.Add(TimeSpan.FromMilliseconds(20))).Token);

            x.Set(lockKey, lc, new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove)
                .SetAbsoluteExpiration(lockExpirationTime)
                .AddExpirationToken(expToken)
                .RegisterPostEvictionCallback((a, b, c, _) =>
                {
                    if (b is not InMemorySessionLock @lock)
                    {
                        return;
                    }

                    switch (c)
                    {
                        case EvictionReason.Capacity:
                            throw new InvalidOperationException(
                                "Locks are being evicted from cache due to capacity limits");
                        case EvictionReason.Replaced:
                            throw new InvalidOperationException(
                                "Locks shouldn't be evicted from cache because of being replaced");
                        case EvictionReason.Expired or EvictionReason.TokenExpired:
                            @lock.SetExpired();
                            break;
                        case EvictionReason.Removed:
                            @lock.SetUnlocked();
                            break;
                    }
                }));

            return lc;
        });

        _logger.LogDebug("Acquiring lock for resource {Resource} with lock ID {Id} and lock key {Key} ended up with status: {Status}",
            resource, lockId, lockKey, acquireResult.Status);

        return acquireResult;
    }

    /// <inheritdoc/>
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        string? lockId = null;
        
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var acquireResult = await AcquirePrivateAsync<TSession>(resource, lockExpirationTime);

            if (lockWaitTime.TotalMilliseconds > 0.0 && lockRetryTime.TotalMilliseconds > 0.0)
            {
                if (!acquireResult.IsAcquired && stopwatch.Elapsed <= lockWaitTime)
                {
                    await Task.Delay(lockRetryTime, ct).WaitAsync(ct);
                }
                
                while (!acquireResult.IsAcquired && stopwatch.Elapsed <= lockWaitTime)
                {
                    _logger.LogDebug("Attempting to retry acquiring lock for {Resource}", resource);
                    
                    acquireResult = await AcquirePrivateAsync<TSession>(resource, lockExpirationTime);
                    if (!acquireResult.IsAcquired)
                    {
                        await Task.Delay(lockRetryTime, ct).WaitAsync(ct);
                    }
                }
            }

            lockId = acquireResult.Id;

            if (!acquireResult.IsAcquired)
            {
                return new SessionLockNotAcquiredError(acquireResult.Status);
            }

            return acquireResult;
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                _logger.LogDebug("Failed to acquire lock for {Resource}, ID: {Id} due to token cancellation", resource, lockId);
            }
            
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, CancellationToken ct = default) where TSession : Session
    {
        var lockId = CreateLockId();

        var acquireResult =  await AcquirePrivateAsync<TSession>(resource, lockExpirationTime);

        if (!acquireResult.IsAcquired)
        {
            return new SessionLockNotAcquiredError(acquireResult.Status);
        }

        return acquireResult;
    }
}