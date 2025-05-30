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
    public string SessionKeyPrefix { get; set; } = "session-tracker";
    
    /// <summary>
    /// Gets the session lock prefix if any.
    /// </summary>
    public string SessionLockPrefix { get; set; } = "lock";
    
    /// <summary>
    /// Gets whether to register memory cache services.
    /// </summary>
    public bool ShouldRegisterMemoryCache { get; set; }
    
    /// <summary>
    /// Gets the action that configures the memory cache options.
    /// </summary>
    public Action<MemoryCacheOptions>? MemoryCacheOptions { get; set; }
}
