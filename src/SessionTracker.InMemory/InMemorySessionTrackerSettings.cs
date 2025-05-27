using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace SessionTracker.InMemory;

/// <summary>
/// The Redis backing store session settings.
/// </summary>
[PublicAPI]
public class InMemorySessionTrackerSettings
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
    public InMemorySessionTrackerSettings SetSessionKeyPrefix(string sessionKeyPrefix)
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
    public InMemorySessionTrackerSettings SetSessionLockPrefix(string sessionLockPrefix)
    {
        if (string.IsNullOrWhiteSpace(sessionLockPrefix))
        {
            throw new InvalidOperationException("Session lock prefix cannot be null or empty.");
        }
        
        SessionLockPrefix = sessionLockPrefix;
        return this;
    }
    
    /// <summary>
    /// Gets whether to register memory cache services.
    /// </summary>
    public bool ShouldRegisterMemoryCache { get; private set; }
    
    /// <summary>
    /// Gets the action that configures the memory cache options.
    /// </summary>
    public Action<MemoryCacheOptions>? MemoryCacheOptions { get; private set; }

    /// <summary>
    /// Specifies that the memory cache services should be registered.
    /// </summary>
    /// <param name="memoryCacheOptions">Memory cache configuration.</param>
    /// <returns>The builder instance.</returns>
    public InMemorySessionTrackerSettings RegisterMemoryCache(Action<MemoryCacheOptions>? memoryCacheOptions = null)
    {
        ShouldRegisterMemoryCache = true;
        MemoryCacheOptions = memoryCacheOptions;
        
        return this;
    }
}
