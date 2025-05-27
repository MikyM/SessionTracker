//
//  RedisSessionLock.cs
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

using JetBrains.Annotations;
using RedLockNet;
using SessionTracker.Abstractions;

namespace SessionTracker.Redis;

/// <summary>
/// A redis based implementation of <see cref="ISessionLock"/>.
/// </summary>
[PublicAPI]
public sealed class RedisSessionLock : ISessionLock, IEquatable<RedisSessionLock>
{
    internal RedisSessionLock(IRedLock redLock)
    {
        _redLock = redLock;
    }

    /// <inheritdoc />
    public string Resource => _redLock.Resource;

    /// <inheritdoc />
    public string Id => _redLock.LockId;

    /// <inheritdoc />
    public bool IsAcquired => _redLock.IsAcquired;

    /// <inheritdoc />
    public SessionLockStatus Status => TranslateRedLockStatus(_redLock.Status);
    
    /// <summary>
    /// Inner RedLock.
    /// </summary>
    private readonly IRedLock _redLock;

    /// <summary>
    /// Translates the status.
    /// </summary>
    /// <param name="status">Status to translate.</param>
    /// <returns>Translated status.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal static SessionLockStatus TranslateRedLockStatus(RedLockStatus status)
        => status switch
        {
            RedLockStatus.Unlocked => SessionLockStatus.Unlocked,
            RedLockStatus.Acquired => SessionLockStatus.Acquired,
            RedLockStatus.NoQuorum => SessionLockStatus.NoQuorum,
            RedLockStatus.Conflicted => SessionLockStatus.Conflicted,
            RedLockStatus.Expired => SessionLockStatus.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _redLock.DisposeAsync();

    /// <inheritdoc />
    public void Dispose()
        => _redLock.Dispose();

    /// <summary>
    /// Base implementation of the equatable.
    /// </summary>
    /// <param name="other">Other.</param>
    /// <returns>Whether two locks are equal.</returns>
    public bool Equals(RedisSessionLock? other)
    {
        if (other is null)
            return false;

        return Resource == other.Resource && Id == other.Id;
    }

    /// <summary>
    /// Base implementation of the equatable.
    /// </summary>
    /// <param name="obj">Other.</param>
    /// <returns>Whether two locks are equal.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((RedisSessionLock)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Resource, Id);
    }

    /// <summary>
    /// Compares two locks.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(RedisSessionLock? left, RedisSessionLock? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two locks.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(RedisSessionLock? left, RedisSessionLock? right)
    {
        return !Equals(left, right);
    }
}
