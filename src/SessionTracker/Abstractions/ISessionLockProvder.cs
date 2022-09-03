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
/// Represents a factory for session locks.
/// </summary>
[PublicAPI]
public interface ISessionLockProvider
{
    /// <summary>
    /// Attempts to acquire a lock for the given session key.
    /// </summary>
    /// <param name="key">Session's key to lock onto.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="lockRetryTime">The time interval in which the attempt will be made to acquire a lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="lockWaitTime">The time before timing out while attempting to acquiring a key.</param>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> AcquireAsync(string key, TimeSpan lockExpirationTime, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime, CancellationToken ct = default);
    
    /// <summary>
    /// Attempts to acquire a lock for the given session key.
    /// </summary>
    /// <param name="key">Session's key to lock onto.</param>
    /// <param name="lockExpirationTime">The lock's expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation that may or not have succeeded.</returns>
    Task<Result<ISessionLock>> AcquireAsync(string key, TimeSpan lockExpirationTime, CancellationToken ct = default);
}
