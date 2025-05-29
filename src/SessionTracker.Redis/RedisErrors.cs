using JetBrains.Annotations;
using Remora.Results;
using StackExchange.Redis;

namespace SessionTracker.Redis;


/// <summary>
/// Error that occurs when Redis returns an unexpected result.
/// </summary>
[PublicAPI]
public record UnexpectedRedisResultError(RedisResult RedisResult) : ResultError("Redis returned an unexpected result.");

/// <summary>
/// Error that occurs when Redis returns an exception.
/// </summary>
[PublicAPI]
public record RedisServerError(RedisServerException RedisServerException) : ResultError("Redis returned an exception.");
