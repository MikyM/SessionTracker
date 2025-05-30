//
//  ISessionTrackerDataProvider.cs
//


using Remora.Results;

namespace SessionTracker.Abstractions;

/// <summary>
/// Represents a factory for session locks.
/// </summary>
[PublicAPI]
public interface ISessionLockProvider
{
    /// <summary>
    /// Attempts to acquire a lock for the given resource key.
    /// </summary>
    /// <param name="resource">Resource's key to lock onto.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// Attempts to acquire a lock for the given resource key.
    /// </summary>
    /// <param name="resource">Resource's key to lock onto.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, CancellationToken ct = default) where TSession : Session;
}
