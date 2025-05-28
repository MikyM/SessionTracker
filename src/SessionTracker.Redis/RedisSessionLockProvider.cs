using JetBrains.Annotations;
using RedLockNet;
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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new instance of <see cref="RedisSessionLockProvider"/>.
    /// </summary>
    /// <param name="lockFactory">Inner factory.</param>
    /// <param name="keyCreator">Key creator.</param>
    /// <param name="timeProvider">Time provider.</param>
    public RedisSessionLockProvider(IDistributedLockFactory lockFactory, RedisSessionTrackerKeyCreator keyCreator, TimeProvider timeProvider)
    {
        _lockFactory = lockFactory;
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
        
        try
        {
            var lockKey = _keyCreator.CreateLockKey<TSession>(resource);

            var lockRes =
                await _lockFactory.CreateLockAsync(lockKey, lockExpirationTime, lockWaitTime, lockRetryTime, ct);
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
        
        try
        {
            var lockKey = _keyCreator.CreateLockKey<TSession>(resource);

            var lockRes = await _lockFactory.CreateLockAsync(lockKey, lockExpirationTime);

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