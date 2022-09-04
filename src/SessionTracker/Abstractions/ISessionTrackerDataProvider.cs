//
//  ISessionTrackerDataProvider.cs
//
//  Author:
//       Krzysztof Kupisz <kupisz.krzysztof@gmail.com>
//
//  Copyright (c) Krzysztof Kupisz
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using Remora.Results;

namespace SessionTracker.Abstractions;

/// <summary>
/// Represents an abstraction between the session tracking service and it's backing store.
/// </summary>
[PublicAPI]
public interface ISessionTrackerDataProvider : IDisposable
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
    /// Attempts to lock the given session key.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan lockExpirationTime, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to lock the given session key.
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
    /// Attempts to remove a session associated with the given key and move it to the evicted backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="evictedExpiration">The evicted entry expiration time.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> EvictAsync<TSession>(string key, TimeSpan evictedExpiration, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key, move it to the evicted backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="evictedExpiration">The evicted entry expiration time.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> EvictAndGetAsync<TSession>(string key, TimeSpan evictedExpiration, CancellationToken ct = default)
        where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the evicted backing store and move it to the regular backing store.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="options">The session's entry options.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result> RestoreAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session;

    /// <summary>
    /// Attempts to remove a session associated with the given key from the evicted backing store, move it to the regular backing store and return the removed value.
    /// </summary>
    /// <remarks>This is guaranteed to be an atomic operation.</remarks>
    /// <param name="key">Session's key to resume.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="options">The session's entry options.</param>
    /// <typeparam name="TSession">Type of the session.</typeparam>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<TSession>> RestoreAndGetAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default)
        where TSession : Session;
}
