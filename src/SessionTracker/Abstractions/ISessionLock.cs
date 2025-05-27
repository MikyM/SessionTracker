//
//  ISessionLock.cs
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

namespace SessionTracker.Abstractions;

/// <summary>
/// Represents a session lock.
/// </summary>
[PublicAPI]
public interface ISessionLock : IAsyncDisposable, IDisposable, IEquatable<ISessionLock>
{
    /// <summary>
    /// The resource on which the lock was acquired.
    /// </summary>
    string Resource { get; }
    
    /// <summary>
    /// Locks ID.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Whether the lock was successfully acquired.
    /// </summary>
    bool IsAcquired { get; }
    
    /// <summary>
    /// The lock's status.
    /// </summary>
    SessionLockStatus Status { get; }

    /// <summary>
    /// Base implementation of the equatable.
    /// </summary>
    /// <param name="other">Other.</param>
    /// <returns>Whether two locks are equal.</returns>
    bool IEquatable<ISessionLock>.Equals(ISessionLock? other) =>
        other is not null && Resource == other.Resource && Id == other.Id;
}
