//
//  ILockedSession.cs
//


namespace SessionTracker.Abstractions;

/// <summary>
/// Represents a locked session.
/// </summary>
/// <typeparam name="TSession">The session type.</typeparam>
[PublicAPI]
public interface ILockedSession<out TSession> where TSession : Session
{
    /// <summary>
    /// A session that is locked.
    /// </summary>
    public TSession Session { get; }
    /// <summary>
    /// Lock associated with the locked session.
    /// </summary>
    ISessionLock Lock { get; }
}
