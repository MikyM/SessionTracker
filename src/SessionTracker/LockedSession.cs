//
//  LockedSession.cs
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
