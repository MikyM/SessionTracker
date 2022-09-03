using JetBrains.Annotations;
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
    private readonly RedLockFactory _lockFactory;

    /// <summary>
    /// Creates a new instance of <see cref="RedisSessionLockProvider"/>.
    /// </summary>
    /// <param name="lockFactory">Inner factory.</param>
    public RedisSessionLockProvider(RedLockFactory lockFactory)
    {
        _lockFactory = lockFactory;
    }


    /// <inheritdoc />
    public async Task<Result<ISessionLock>> AcquireAsync(string key, TimeSpan lockExpirationTime, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime,
        CancellationToken ct = default)
    {
        var lockRes = await _lockFactory.CreateLockAsync(key, lockExpirationTime, lockWaitTime, lockRetryTime, ct).ConfigureAwait(false);
        if (!lockRes.IsAcquired)
            return new SessionLockNotAcquiredError(RedisSessionLock.TranslateRedLockStatus(lockRes.Status));

        return new RedisSessionLock(lockRes);
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> AcquireAsync(string key, TimeSpan lockExpirationTime,
        CancellationToken ct = default)
    {
        var lockRes = await _lockFactory.CreateLockAsync(key, lockExpirationTime).ConfigureAwait(false);
        ;
        if (!lockRes.IsAcquired)
            return new SessionLockNotAcquiredError(RedisSessionLock.TranslateRedLockStatus(lockRes.Status));

        return new RedisSessionLock(lockRes);
    }
}
