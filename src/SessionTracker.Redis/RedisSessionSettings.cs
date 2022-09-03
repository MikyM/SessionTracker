using JetBrains.Annotations;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace SessionTracker.Redis;

/// <summary>
/// The Redis backing store session settings.
/// </summary>
[PublicAPI]
public class RedisSessionSettings
{
    /// <summary>
    /// Gets the Session key storage prefix.
    /// </summary>
    public string KeyPrefix => "sessions";
    
    /// <summary>
    /// The multiplexer, if any.
    /// </summary>
    public IConnectionMultiplexer? Multiplexer { get; set; }
    
    /// <summary>
    /// The multiplexer factory, if any.
    /// </summary>
    public Func<IConnectionMultiplexer>? MultiplexerFactory { get; set; }
    
    /// <summary>
    /// The redis connection configuration, if any.
    /// </summary>
    public Action<ConfigurationOptions>? RedisConfigurationOptions { get; set; }

    /// <summary>
    /// The Redis instance name.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// The Redis profiling session
    /// </summary>
    public Func<ProfilingSession>? ProfilingSession { get; set; }
    
    /// <summary>
    /// The name of the Json serializer used to serialize sessions.
    /// </summary>
    public const string JsonSerializerName = "SessionsJson";
}
