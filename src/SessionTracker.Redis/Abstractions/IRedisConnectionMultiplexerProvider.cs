namespace SessionTracker.Redis.Abstractions;

/// <summary>
/// Represents a connection provider.
/// </summary>
[PublicAPI]
public interface IRedisConnectionMultiplexerProvider
{
    /// <summary>
    /// Provides a Redis connection multiplexer.
    /// </summary>
    /// <returns>An instance of <see cref="IConnectionMultiplexer"/>.</returns>
    ValueTask<IConnectionMultiplexer> GetConnectionMultiplexerAsync();    
    
    /// <summary>
    /// Gets the configuration options.
    /// </summary>
    ValueTask<ConfigurationOptions> GetConfigurationOptionsAsync();   
}