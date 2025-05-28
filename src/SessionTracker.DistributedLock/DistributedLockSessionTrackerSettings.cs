using JetBrains.Annotations;

namespace SessionTracker.DistributedLock;

/// <summary>
/// Settings of the sub-library translation layer. 
/// </summary>
[PublicAPI]
public class DistributedLockSessionTrackerSettings
{
    /// <summary>
    /// Gets the session key prefix if any.
    /// </summary>
    public string SessionKeyPrefix { get; private set; } = "session-tracker";
    
    /// <summary>
    /// Gets the session lock prefix if any.
    /// </summary>
    public string SessionLockPrefix { get; private set; } = "lock";
    
    /// <summary>
    /// Sets the session key prefix.
    /// </summary>
    /// <param name="sessionKeyPrefix">Session key prefix.</param>
    /// <returns>The settings.</returns>
    public DistributedLockSessionTrackerSettings SetSessionKeyPrefix(string sessionKeyPrefix)
    {
        if (string.IsNullOrWhiteSpace(sessionKeyPrefix))
        {
            throw new InvalidOperationException("Session key prefix cannot be null or empty.");
        }
        
        SessionKeyPrefix = sessionKeyPrefix;
        return this;
    }

    /// <summary>
    /// Sets the session lock prefix.
    /// </summary>
    /// <param name="sessionLockPrefix">Session lock prefix.</param>
    /// <returns>The settings.</returns>
    public DistributedLockSessionTrackerSettings SetSessionLockPrefix(string sessionLockPrefix)
    {
        if (string.IsNullOrWhiteSpace(sessionLockPrefix))
        {
            throw new InvalidOperationException("Session lock prefix cannot be null or empty.");
        }
        
        SessionLockPrefix = sessionLockPrefix;
        return this;
    }
}