using Remora.Results;
using SessionTracker.Abstractions;
using SessionTracker.Redis.Abstractions;

namespace SessionTracker.Redis;

/// <summary>
/// A redis based implementation of the <see cref="ISessionLockProvider"/>.
/// </summary>
[PublicAPI]
public sealed class RedisSessionLockProvider : ISessionLockProvider
{
    private readonly IDistributedLockFactoryProvider _lockFactoryProvider;
    private readonly RedisSessionTrackerKeyCreator _keyCreator;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new instance of <see cref="RedisSessionLockProvider"/>.
    /// </summary>
    /// <param name="lockFactoryProvider">Inner factory.</param>
    /// <param name="keyCreator">Key creator.</param>
    /// <param name="timeProvider">Time provider.</param>
    public RedisSessionLockProvider(IDistributedLockFactoryProvider lockFactoryProvider, RedisSessionTrackerKeyCreator keyCreator, TimeProvider timeProvider)
    {
        _lockFactoryProvider = lockFactoryProvider;
        _keyCreator = keyCreator;
        _timeProvider = timeProvider;
    }


    /// <inheritdoc />
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime,
        TimeSpan lockWaitTime,
        TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var factory = await _lockFactoryProvider.GetDistributedLockFactoryAsync();
        
        try
        {
            var lockKey = _keyCreator.CreateLockKey<TSession>(resource);

            var lockRes = await factory.CreateLockAsync(lockKey, lockExpirationTime, lockWaitTime, lockRetryTime, ct);
            if (!lockRes.IsAcquired)
                return new SessionLockNotAcquiredError(RedisSessionLock.TranslateRedLockStatus(lockRes.Status));

            return new RedisSessionLock(lockRes,_timeProvider.GetUtcNow().Add(lockExpirationTime));
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
        
        var factory = await _lockFactoryProvider.GetDistributedLockFactoryAsync();
        
        try
        {
            var lockKey = _keyCreator.CreateLockKey<TSession>(resource);

            var lockRes = await factory.CreateLockAsync(lockKey, lockExpirationTime);

            if (!lockRes.IsAcquired)
                return new SessionLockNotAcquiredError(RedisSessionLock.TranslateRedLockStatus(lockRes.Status));

            return new RedisSessionLock(lockRes,_timeProvider.GetUtcNow().Add(lockExpirationTime));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}