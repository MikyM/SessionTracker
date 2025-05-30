namespace SessionTracker.Abstractions;

/// <summary>
/// Represents underlying data provider session key.
/// </summary>
[PublicAPI]
public interface ISessionProviderKey
{
    /// <summary>
    /// The actual Id.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Whether this key represents an evicted data store.
    /// </summary>
    bool IsEvicted { get; }
}