using JetBrains.Annotations;
using Remora.Results;
using StackExchange.Redis;

namespace SessionTracker.Redis;

/// <summary>
/// Errors.
/// </summary>
[PublicAPI]
public static class RedisErrors
{
    /// <summary>
    /// Error that occurs when Redis returns an unexpected result.
    /// </summary>
    public record UnexpectedRedisResultError(RedisResult RedisResult) : ResultError("Redis returned an unexpected result.");
}
