﻿//
//  Session.cs
//


using System.Text.Json;
using System.Text.Json.Serialization;
using SessionTracker.Abstractions;

namespace SessionTracker;

/// <summary>
/// Represents a basic session.
/// </summary>
[PublicAPI]
public class Session : ISession, IEquatable<ISession>
{
    /// <summary>
    /// Base session constructor.
    /// </summary>
    /// <param name="key">Clients Sessions key.</param>
    public Session(string key)
    {
        Key = key;
    }

    /// <summary>
    /// The version of the session.
    /// </summary>
    [JsonInclude]
    public long Version { get; internal set; } = 1;

    /// <inheritdoc/>
    public void SetProviderKeys(string regularKey, string evictedKey)
    {
        ProviderKey = regularKey;
        EvictedProviderKey = evictedKey;
    }

    /// <summary>
    /// Gets the time at which this session started.
    /// </summary>
    [JsonInclude]
    public DateTimeOffset StartedAt { get; internal set; } = DateTimeProvider.Instance.OffsetUtcNow;

    /// <summary>
    /// The clients key associated with this session.
    /// </summary>
    [JsonInclude]
    public string Key { get; internal set; }
    
    /// <summary>
    /// The underlying provider key associated with this session.
    /// </summary>
    [JsonInclude]
    public string? ProviderKey { get; internal set; }
    
    /// <summary>
    /// The underlying provider cache key associated with this session.
    /// </summary>
    [JsonInclude]
    public string? EvictedProviderKey { get; internal set; }

    /// <summary>
    /// Serializes this instance to JSON.
    /// </summary>
    /// <returns>JSON representation of current instance.</returns>
    public override string ToString()
        => JsonSerializer.Serialize(this);

    /// <inheritdoc />
    public bool Equals(ISession? other)
    {
        if (other is null)
            return false;

        return Key == other.Key;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Session)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        => Key.GetHashCode();
    
    /// <summary>
    /// Compares two locked sessions.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Session? left, Session? right)
        => Equals(left, right);
    
    /// <summary>
    /// Compares two locked sessions.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Session? left, Session? right)
        => !Equals(left, right);
}
