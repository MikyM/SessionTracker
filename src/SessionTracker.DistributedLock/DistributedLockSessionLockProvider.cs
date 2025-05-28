using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Remora.Results;
using SessionTracker.Abstractions;

namespace SessionTracker.DistributedLock;

/// <summary>
/// Implementation of <see cref="ISessionLockProvider"/> based on the distributed lock abstraction layer.
/// </summary>
[PublicAPI]
public class DistributedLockSessionLockProvider : ISessionLockProvider
{
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly ILogger<DistributedLockSessionLockProvider> _logger;
    private readonly DistributedLockNameCreator _nameCreator;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new instance of <see cref="DistributedLockSessionLockProvider"/>/
    /// </summary>
    /// <param name="distributedLockProvider">The underlying lock provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="nameCreator">The lock name creator.</param>
    /// <param name="timeProvider">The time provider.</param>
    public DistributedLockSessionLockProvider(IDistributedLockProvider distributedLockProvider, ILogger<DistributedLockSessionLockProvider> logger, 
        DistributedLockNameCreator nameCreator, TimeProvider timeProvider)
    {
        _distributedLockProvider = distributedLockProvider;
        _logger = logger;
        _nameCreator = nameCreator;
        _timeProvider = timeProvider;
    }

    private static string CreateLockId()
        => Guid.NewGuid().ToString();

    /// <inheritdoc/>
    /// <remarks>
    /// Due to the abstraction layer provider by <see cref="IDistributedLockProvider"/> both <paramref name="lockExpirationTime"></paramref> and <paramref name="lockRetryTime"></paramref> values will be ignored.
    /// These are configurable from the options area provided by the DistributedLock libraries.
    /// </remarks>
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        var name = _nameCreator.CreateName<TSession>(resource);
        
        var @lock = _distributedLockProvider.CreateLock(name);
        
        var result = await @lock.TryAcquireAsync(lockWaitTime, ct);
        var expAt = _timeProvider.GetUtcNow().Add(lockExpirationTime);

        return result is null
            ? new SessionLockNotAcquiredError(SessionLockStatus.Conflicted)
                : new DistributedLockSessionLock(result, expAt, resource, true, SessionLockStatus.Acquired, CreateLockId());
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Due to the abstraction layer provider by <see cref="IDistributedLockProvider"/> <paramref name="lockExpirationTime"></paramref> value will be ignored.
    /// It's configurable from the options area provided by the DistributedLock libraries.
    /// </remarks>
    public async Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, CancellationToken ct = default) where TSession : Session
    {
        var name = _nameCreator.CreateName<TSession>(resource);
        
        var @lock = _distributedLockProvider.CreateLock(name);
        
        var result = await @lock.TryAcquireAsync(TimeSpan.Zero, ct);
        var expAt = _timeProvider.GetUtcNow().Add(lockExpirationTime);

        return result is null
            ? new SessionLockNotAcquiredError(SessionLockStatus.Conflicted)
            : new DistributedLockSessionLock(result, expAt, resource, true, SessionLockStatus.Acquired, CreateLockId());
    }
}