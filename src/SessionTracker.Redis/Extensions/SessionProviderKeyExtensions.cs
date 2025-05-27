using JetBrains.Annotations;
using SessionTracker.Abstractions;
using StackExchange.Redis;

namespace SessionTracker.Redis.Extensions;

/// <summary>
/// Extensions for <see cref="ISessionProviderKey"/>
/// </summary>
[PublicAPI]
public static class SessionProviderKeyExtensions
{
    /// <summary>
    /// Transforms a <see cref="ISessionProviderKey"/> to <see cref="RedisValue"/>.
    /// </summary>
    /// <param name="key">The key to transform.</param>
    /// <returns>The resulting <see cref="RedisValue"/>.</returns>
    public static RedisValue ToRedisValue(this ISessionProviderKey key)
        => key.Id;
    
    /// <summary>
    /// Transforms a <see cref="ISessionProviderKey"/> to <see cref="RedisKey"/>.
    /// </summary>
    /// <param name="key">The key to transform.</param>
    /// <returns>The resulting <see cref="RedisKey"/>.</returns>
    public static RedisKey ToRedisKey(this ISessionProviderKey key)
        => key.Id;
}