//
//  RedisSessionSettingsExtensions.cs
//


using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using SessionTracker.Abstractions;
using SessionTracker.Redis.Abstractions;

namespace SessionTracker.Redis;

// ReSharper disable once InconsistentNaming
/// <summary>
/// DI extensions.
/// </summary>
[PublicAPI]
public static class SessionTrackerBuilderExtensions
{
    /// <summary>
    /// Adds a redis based backing store implementation for sessions.
    /// </summary>
    /// <remarks>
    /// It is very important to know that the stock implementation of <see cref="ISessionDataProvider"/> that this method
    /// adds uses JSON to store values. If JSON is not a desirable format, the caching methods in <see cref="RedisSessionDataProvider"/> can be provided, or a custom
    /// implementation of <see cref="ISessionDataProvider"/> can be added to the container.
    /// </remarks>
    /// <param name="builder">The builder.</param>
    /// <param name="redisSessionConfiguration">Redis session tracker configuration.</param>
    /// <returns>The options.</returns>
    public static RedisSessionTrackerBuilder AddRedisProviders
    (
        this SessionTrackerBuilder builder, Action<RedisSessionTrackerSettings> redisSessionConfiguration
    )
    {
        var redisOpt = new RedisSessionTrackerSettings();
        redisSessionConfiguration(redisOpt);

        builder.Services.AddOptions();
        
        if (redisOpt.JsonSerializerConfiguration is not null)
        {
            builder.Services.Configure(RedisSessionTrackerSettings.JsonSerializerName, redisOpt.JsonSerializerConfiguration);
        }
        
        builder.Services.Configure(redisSessionConfiguration);
        
        builder.Services.TryAddSingleton(TimeProvider.System);
        
        builder.Services.TryAddSingleton<IRedisConnectionMultiplexerProvider, RedisConnectionMultiplexerProvider>();
        
        builder.Services.TryAddSingleton<IDistributedLockFactoryProvider, DistributedLockFactoryProvider>();
        
        builder.AddLockProvider<RedisSessionLockProvider>();
        
        builder.AddDataProvider<RedisSessionDataProvider>();

        builder.Services.TryAddSingleton<RedisSessionTrackerKeyCreator>();
        
        builder.Services.AddLogging();

        return new RedisSessionTrackerBuilder(builder);
    }
}
