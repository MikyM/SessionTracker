//
//  SessionLockStatus.cs
//


namespace SessionTracker;

/// <summary>
/// Lock status.
/// </summary>
[PublicAPI]
public enum SessionLockStatus
{
    /// <summary>
    /// Unlocked.
    /// </summary>
    Unlocked,
    /// <summary>
    /// Acquired.
    /// </summary>
    Acquired,
    /// <summary>
    /// NoQuorum.
    /// </summary>
    NoQuorum,
    /// <summary>
    /// Conflicted.
    /// </summary>
    Conflicted,
    /// <summary>
    /// Expired.
    /// </summary>
    Expired,
}
