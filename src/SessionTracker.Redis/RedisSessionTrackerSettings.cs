using System.Text.Json;
using JetBrains.Annotations;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace SessionTracker.Redis;

/// <summary>
/// The Redis backing store session settings.
/// </summary>
[PublicAPI]
public class RedisSessionTrackerSettings
{
    /// <summary>
    /// Gets whether lock factory creation was skipped.
    /// </summary>
    public bool SkipLockFactoryCreation { get; init; }
    
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
    public string SessionKeyPrefix { get; init; } = "session-tracker";
    
    /// <summary>
    /// Gets the session lock prefix if any.
    /// </summary>
    public string SessionLockPrefix { get; init; } = "lock";
    
    /// <summary>
    /// The JSON serializer configuration.
    /// </summary>
    public Action<JsonSerializerOptions>? JsonSerializerConfiguration { get; init; }
    
    /// <summary>
    /// The multiplexer, if any.
    /// </summary>
    public IConnectionMultiplexer? Multiplexer { get; init; }
    
    /// <summary>
    /// The multiplexer factory, if any.
    /// </summary>
    public Func<IConnectionMultiplexer>? MultiplexerFactory { get; init; }
    
    /// <summary>
    /// The redis connection configuration, if any.
    /// </summary>
    public ConfigurationOptions? RedisConfigurationOptions { get; init; }

    /// <summary>
    /// The Redis profiling session
    /// </summary>
    public Func<ProfilingSession>? ProfilingSession { get; init; }
    
    /// <summary>
    /// The name of the Json serializer used to serialize sessions.
    /// </summary>
    public const string JsonSerializerName = "RedisSessionTrackerJsonOptions";
}
