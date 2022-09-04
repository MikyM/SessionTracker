//
//  Errors.cs
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
using SessionTracker.Abstractions;

namespace SessionTracker;

/// <summary>
/// Error that occurs when attempting to start a session when there is an existing non-finished session associated with the given key.
/// </summary>
[PublicAPI]
public record SessionInProgressError(ISession Session) : ResultError("There is a session cached with the same key that is in progress. See Session for details.");

/// <summary>
/// Error that occurs when attempting to update a session when the session associated with the given key is already evicted.
/// </summary>
[PublicAPI]
public record SessionAlreadyEvictedError() : ResultError("Session associated with the given key has been evicted.");

/// <summary>
/// Error that occurs when attempting to restore a session when the session associated with the given key is already restored.
/// </summary>
[PublicAPI]
public record SessionAlreadyRestoredError() : ResultError("Session associated with the given key has already been restored.");

/// <summary>
/// Error that occurs when a lock for a Session couldn't be acquired.
/// </summary>
[PublicAPI]
public record SessionLockNotAcquiredError(SessionLockStatus Status) : ResultError("Couldn't obtain a lock for the given session. Check inner lock entry for details.");

