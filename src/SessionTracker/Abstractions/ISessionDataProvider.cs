//
//  ISessionTrackerDataProvider.cs
//


using Remora.Results;

namespace SessionTracker.Abstractions;

/// <summary>
/// Represents an abstraction between the session tracking service and it's backing store.
/// </summary>
[PublicAPI]
public interface ISessionDataProvider
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
    Task<Result<TSession>> GetEvictedAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session;

    /// <summary>
    /// Attempts to add a session to the underlying backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="session">The session to add.</param>
    /// <param name="options">The session's entry options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> AddAsync<TSession>(TSession session, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to refresh session's sliding expiration.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
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
    /// Attempts to remove a session associated with the given key and move it to the evicted backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="options">The session's entry evicted options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> EvictAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key, move it to the evicted backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="options">The session's entry evicted options.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> EvictAndGetAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default)
        where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the evicted backing store and move it to the regular backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="options">The session's entry options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> RestoreAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the evicted backing store, move it to the regular backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="options">The session's entry options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> RestoreAndGetAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default)
        where TSession : Session;
}
