using JetBrains.Annotations;
using Remora.Results;

namespace SessionTracker.Redis;

/// <summary>
/// Represents an error that occurs when the proxy script optimization is used and the <see cref="RedisSessionTrackerSettings.BandwidthOptimizationNoScriptRetriesLimit"/> has been reached.
/// </summary>
[PublicAPI]
public sealed record RedisBandwidthScriptOptimizationError() : ResultError("Failed to utilize bandwidth script optimization - NOSCRIPT retry count reached.");