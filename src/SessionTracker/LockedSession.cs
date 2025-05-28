//
//  LockedSession.cs
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


using SessionTracker.Abstractions;

namespace SessionTracker;

/// <summary>
/// Represents a locked session.
/// </summary>
/// <typeparam name="TSession">The session type.</typeparam>
[PublicAPI]
public sealed class LockedSession<TSession> : ILockedSession<TSession>, IEquatable<LockedSession<TSession>>, IDisposable, IAsyncDisposable, IEquatable<ILockedSession<TSession>> where TSession : Session
{
    internal LockedSession(TSession session, ISessionLock @lock)
    {
        Session = session;
        Lock = @lock;
    }


    /// <inheritdoc />
    public TSession Session { get; }

    /// <inheritdoc />
    public ISessionLock Lock { get; }

    /// <summary>
    /// Releases the lock associated with the locked session.
    /// </summary>
    public async ValueTask DisposeAsync()
        => await Lock.DisposeAsync();
    
    /// <summary>
    /// Releases the lock associated with the locked session.
    /// </summary>
    public void Dispose()
        => Lock.Dispose();

    /// <inheritdoc />
    public bool Equals(ILockedSession<TSession>? other)
    {
        if (other is null)
            return false;
        
        return Session.Equals(other.Session) && Lock.Equals(other.Lock);
    }

    /// <inheritdoc />
    public bool Equals(LockedSession<TSession>? other)
    {
        if (other is null)
            return false;
        
        return Session.Equals(other.Session) && Lock.Equals(other.Lock);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is LockedSession<TSession> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Session, Lock);
    }

    /// <summary>
    /// Compares two locked sessions.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(LockedSession<TSession>? left, LockedSession<TSession>? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two locked sessions.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(LockedSession<TSession>? left, LockedSession<TSession>? right)
    {
        return !Equals(left, right);
    }
}
