using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
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

    /// <summary>
    /// Creates a new instance of <see cref="InMemorySessionLockProvider"/>.
    /// </summary>
    /// <param name="cacheQueue">The cache queue.</param>
    /// <param name="keyCreator">Key creator.</param>
    public InMemorySessionLockProvider(MemoryCacheQueue cacheQueue, InMemorySessionTrackerKeyCreator keyCreator)
    {
        _cacheQueue = cacheQueue;
        _keyCreator = keyCreator;
    }

    private async Task<InMemorySessionLock> AcquirePrivateAsync<TSession>(string resource, TimeSpan lockExpirationTime) where TSession : Session
    {
        var lockId = CreateLockId();
        var lockKey = _keyCreator.CreateLockKey<TSession>(resource);
        
        var acquireResult = await _cacheQueue.Enqueue(x =>
        {
            var exists = x.TryGetValue(lockKey, out _);

            if (exists)
            {
                return new InMemorySessionLock(lockKey, lockId, false, SessionLockStatus.Conflicted, null, lockExpirationTime);
            }
            
            x.Set(lockKey, lockId, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = lockExpirationTime
            });
            
            return new InMemorySessionLock(lockKey, lockId, true, SessionLockStatus.Acquired, _cacheQueue, lockExpirationTime);
        });
        
        return acquireResult;
    }

    /// <inheritdoc/>
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        var stopwatch = Stopwatch.StartNew();
            
        var acquireResult = await AcquirePrivateAsync<TSession>(resource, lockExpirationTime);
        
        if (lockWaitTime.TotalMilliseconds > 0.0 && lockRetryTime.TotalMilliseconds > 0.0)
        {
            while (!acquireResult.IsAcquired && stopwatch.Elapsed <= lockWaitTime)
            {
                acquireResult = await AcquirePrivateAsync<TSession>(resource, lockExpirationTime);
                if (!acquireResult.IsAcquired)
                {
                    await Task.Delay(lockRetryTime, ct).WaitAsync(ct);
                }
            }
        }

        if (!acquireResult.IsAcquired)
        {
            return new SessionLockNotAcquiredError(acquireResult.Status);
        }

        return acquireResult;
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