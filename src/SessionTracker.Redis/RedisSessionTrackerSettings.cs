using System.Text.Json;
using StackExchange.Redis.Profiling;

namespace SessionTracker.Redis;

/// <summary>
/// The Redis backing store session settings.
/// </summary>
[PublicAPI]
public class RedisSessionTrackerSettings
{
    /// <summary>
    /// Gets or set which redis database to use.
    /// </summary>
    public int Database { get; set; } = -1;
    
    /// <summary>
    /// Whether to use bandwidth optimization when using a proxy.
    /// </summary>
    public bool UseBandwidthOptimizationForProxies { get; set; } = true;

    /// <summary>
    /// Gets the amount of times a scrip execution will be retried if the result is NOSCRIPT.
    /// </summary>
    public int BandwidthOptimizationNoScriptRetriesLimit { get; set; } = 5;
    
    /// <summary>
    /// Gets the session key prefix if any.
    /// </summary>
    public string SessionKeyPrefix { get; set; } = "session-tracker";
    
    /// <summary>
    /// Gets the session lock prefix if any.
    /// </summary>
    public string SessionLockPrefix { get; set; } = "lock";
    
    /// <summary>
    /// The JSON serializer configuration.
    /// </summary>
    public Action<JsonSerializerOptions>? JsonSerializerConfiguration { get; set; }
    
    /// <summary>
    /// The multiplexer factory, if any.
    /// </summary>
    public Func<Task<IConnectionMultiplexer>>? MultiplexerFactory { get; set; }
    
    /// <summary>
    /// The redis connection configuration, if any.
    /// </summary>
    public ConfigurationOptions? RedisConfigurationOptions { get; set; }

    /// <summary>
    /// The Redis profiling session
    /// </summary>
    public Func<ProfilingSession>? ProfilingSession { get; set; }
    
    /// <summary>
    /// The name of the Json serializer used to serialize sessions.
    /// </summary>
    public const string JsonSerializerName = "RedisSessionTrackerJsonOptions";
}
