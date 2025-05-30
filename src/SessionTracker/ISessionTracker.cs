//
//  ISessionTracker.cs
//



using Remora.Results;
using SessionTracker.Abstractions;

namespace SessionTracker;

/// <summary>
/// Represents the session tracking service.
/// <para>
/// Allows starting, finishing, updating, retrieving and locking sessions.
/// </para>
/// </summary>
[PublicAPI]
public interface ISessionTracker
{
    /// <summary>
    /// Attempts to get a session associated with the given key from the underlying backing store and refreshes it's sliding expiration if any.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> GetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session;
    
    /// <summary>
    /// Attempts to get a session associated with the given key from the underlying evicted backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> GetFinishedAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session;

    /// <summary>
    /// <para>
    /// Attempts to lock the given session key, gets the session associated with the key from the underlying backing store and refreshes it's sliding expiration if any.
    /// </para>
    /// <para>
    /// This method will use either a configured type-specific lock expiration time or a default lock expiration time if none is passed.
    /// </para>
    /// </summary>
    /// <remarks>This is not guaranteed to be an atomic operation - it may consist of acquiring a lock and retrieving the session.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ILockedSession<TSession>>> GetLockedAsync<TSession>(string key, TimeSpan? lockExpirationTime = null,
        CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to lock the given session key, gets the session associated with the key from the underlying backing store and refreshes it's sliding expiration if any.
    /// </summary>
    /// <remarks>This is not guaranteed to be an atomic operation - it may consist of acquiring a lock and retrieving the session.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ILockedSession<TSession>>> GetLockedAsync<TSession>(string key, TimeSpan lockExpirationTime, TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// <para>
    /// Attempts to lock the given session key, gets the session associated with the key from the underlying backing store and refreshes it's sliding expiration if any.
    /// </para>
    /// <para>
    /// This method will use either a configured type-specific lock expiration time or a default lock expiration time.
    /// </para>
    /// </summary>
    /// <remarks>This is not guaranteed to be an atomic operation - it may consist of acquiring a lock and retrieving the session.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ILockedSession<TSession>>> GetLockedAsync<TSession>(string key, TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to add a session to the underlying backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">The session to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> StartAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to add a session to the underlying backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">The session to add.</param>
    /// <param name="options">Entry options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> StartAsync<TSession>(TSession session, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to refresh session's sliding expiration.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">The session to refresh the expiration for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> RefreshAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// Attempts to refresh session's sliding expiration.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">The key of the session to refresh the expiration for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> RefreshAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// Attempts to update a session and refresh it's sliding expiration if any.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">The session to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> UpdateAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// Attempts to update a session and retrieve the just updated value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">The session to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> UpdateAndGetAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// <para>
    /// Attempts to lock the given session key.
    /// </para>
    /// <para>
    /// This method will use either a configured type-specific lock expiration time or a default lock expiration time if none is passed.
    /// </para>
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan? lockExpirationTime = null, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// <para>
    /// Attempts to lock the given session key.
    /// </para>
    /// <para>
    /// This method will use either a configured type-specific lock expiration time or a default lock expiration time.
    /// </para>
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan lockWaitTime, TimeSpan lockRetryTime, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// <para>
    /// Attempts to lock the given session key.
    /// </para>
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan lockExpirationTime, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// <para>
    /// Attempts to lock the given session key.
    /// </para>
    /// <para>
    /// This method will use either a configured type-specific lock expiration time or a default lock expiration time if none is passed.
    /// </para>
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">Session to lock.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan? lockExpirationTime = null,
        CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// <para>
    /// Attempts to lock the given session key.
    /// </para>
    /// <para>
    /// This method will use either a configured type-specific lock expiration time or a default lock expiration time.
    /// </para>
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">Session to lock.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// <para>
    /// Attempts to lock the given session key.
    /// </para>
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">Session to lock.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan lockExpirationTime, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// Attempts to remove a session from the backing store and move it to the evicted backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">Session to lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> FinishAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// Attempts to remove a session associated with the given key from the backing store and move it to the evicted backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> FinishAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the backing store, move it to the evicted backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">Session to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> FinishAndGetAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the backing store, move it to the evicted backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> FinishAndGetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session;
    
    /// <summary>
    /// Attempts to remove a session from the evicted backing store and move it to the regular backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">Session to resume.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> ResumeAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;
    
    /// <summary>
    /// Attempts to remove a session associated with the given key from the evicted backing store and move it to the regular backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> ResumeAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the evicted backing store, move it to the regular backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">Session to resume.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> ResumeAndGetAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the evicted backing store, move it to the regular backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key to resume.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> ResumeAndGetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session;
}
