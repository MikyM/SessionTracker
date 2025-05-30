//
//  ISession.cs
//


using System.Diagnostics.CodeAnalysis;

namespace SessionTracker.Abstractions;

/// <summary>
/// Represents a basic session.
/// </summary>
[PublicAPI]
public interface ISession
{
    /// <summary>
    /// Session's Id.
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// The underlying provider key associated with this session.
    /// </summary>
    string? ProviderKey { get; }
    
    /// <summary>
    /// The underlying evicted provider key associated with this session.
    /// </summary>
    string? EvictedProviderKey { get; }

    /// <summary>
    /// Gets the time at which this session started.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// The version of the session for concurrent operation checks.
    /// </summary>
    long Version { get; }

    /// <summary>
    /// Sets the provider keys.
    /// </summary>
    /// <param name="regularKey">Regular session key in the underlying storage.</param>
    /// <param name="evictedKey">Evicted session key in the underlying storage.</param>
    [MemberNotNull(nameof(ProviderKey), nameof(EvictedProviderKey))]
    void SetProviderKeys(string regularKey, string evictedKey);
}
