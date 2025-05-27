using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace SessionTracker.InMemory;

/// <summary>
/// Default Session key creator
/// </summary>
[PublicAPI]
public sealed class InMemorySessionTrackerKeyCreator
{
    private readonly IOptions<InMemorySessionTrackerSettings> _options;

    /// <summary>
    /// Creates a new instance of <see cref="InMemorySessionTrackerKeyCreator"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    public InMemorySessionTrackerKeyCreator(IOptions<InMemorySessionTrackerSettings> options)
    {
        _options = options;
    }

    /// <summary>
    /// Creates a new lock key for a given session.
    /// </summary>
    /// <param name="initKey">Initial key.</param>
    /// <typeparam name="TSession">The session type.</typeparam>
    /// <returns>Created key.</returns>
    public string CreateLockKey<TSession>(string initKey) where TSession : Session
        => $"{_options.Value.SessionKeyPrefix}:{_options.Value.SessionLockPrefix}:{typeof(TSession).Name.ToLower()}:{initKey}";

    /// <summary>
    /// Creates a new key for a given session.
    /// </summary>
    /// <param name="initKey">Initial key.</param>
    /// <typeparam name="TSession">The session type.</typeparam>
    /// <returns>Created key.</returns>
    public string CreateKey<TSession>(string initKey) where TSession : Session
        => $"{_options.Value.SessionKeyPrefix}:{typeof(TSession).Name.ToLower()}:{initKey}";
    
    /// <summary>
    /// Creates a new evicted key for a given session.
    /// </summary>
    /// <param name="initKey">Initial key.</param>
    /// <typeparam name="TSession">The session type.</typeparam>
    /// <returns>Created key.</returns>
    public string CreateEvictedKey<TSession>(string initKey) where TSession : Session
        => $"{_options.Value.SessionKeyPrefix}:evicted:{typeof(TSession).Name.ToLower()}:{initKey}";

    /// <summary>
    /// Creates regular and evicted keys for a given session.
    /// </summary>
    /// <param name="initKey">Initial key.</param>
    /// <typeparam name="TSession">The session type.</typeparam>
    /// <returns>Created key.</returns>
    public (string Regular, string Evicted) CreateKeys<TSession>(string initKey) where TSession : Session
        => new(CreateKey<TSession>(initKey), CreateEvictedKey<TSession>(initKey));
}