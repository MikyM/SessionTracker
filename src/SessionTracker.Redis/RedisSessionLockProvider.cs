using JetBrains.Annotations;
using RedLockNet;
using RedLockNet.SERedis;
using Remora.Results;
using SessionTracker.Abstractions;

namespace SessionTracker.Redis;

/// <summary>
/// A redis based implementation of the <see cref="ISessionLockProvider"/>.
/// </summary>
[PublicAPI]
public sealed class RedisSessionLockProvider : ISessionLockProvider
{
    private readonly IDistributedLockFactory _lockFactory;
    private readonly RedisSessionTrackerKeyCreator _keyCreator;

    /// <summary>
    /// Creates a new instance of <see cref="RedisSessionLockProvider"/>.
    /// </summary>
    /// <param name="lockFactory">Inner factory.</param>
    /// <param name="keyCreator">Key creator.</param>
    public RedisSessionLockProvider(IDistributedLockFactory lockFactory, RedisSessionTrackerKeyCreator keyCreator)
    {
        _lockFactory = lockFactory;
        _keyCreator = keyCreator;
    }


    /// <inheritdoc />
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime,
        TimeSpan lockWaitTime,
        TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            var lockKey = _keyCreator.CreateLockKey<TSession>(resource);

            var lockRes =
                await _lockFactory.CreateLockAsync(lockKey, lockExpirationTime, lockWaitTime, lockRetryTime, ct);
            if (!lockRes.IsAcquired)
                return new SessionLockNotAcquiredError(RedisSessionLock.TranslateRedLockStatus(lockRes.Status));

            return new RedisSessionLock(lockRes,DateTimeOffset.UtcNow.Add(lockExpirationTime));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            var lockKey = _keyCreator.CreateLockKey<TSession>(resource);

            var lockRes = await _lockFactory.CreateLockAsync(lockKey, lockExpirationTime);

            if (!lockRes.IsAcquired)
                return new SessionLockNotAcquiredError(RedisSessionLock.TranslateRedLockStatus(lockRes.Status));

            return new RedisSessionLock(lockRes,DateTimeOffset.UtcNow.Add(lockExpirationTime));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}