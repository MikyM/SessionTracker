using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace SessionTracker.DistributedLock;

/// <summary>
/// Creates names for locks.
/// </summary>
[PublicAPI]
public class DistributedLockNameCreator
{
    private readonly IOptions<DistributedLockSessionTrackerSettings> _options;

    /// <summary>
    /// Creates a new instance of <see cref="DistributedLockNameCreator"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    public DistributedLockNameCreator(IOptions<DistributedLockSessionTrackerSettings> options)
    {
        _options = options;
    }

    /// <summary>
    /// Creates a name for a lock.
    /// </summary>
    /// <param name="initKey">The initial key.</param>
    /// <typeparam name="TSession">The type of the session.</typeparam>
    /// <returns>The created lock name</returns>
    public string CreateName<TSession>(string initKey) where TSession : Session
        => $"{_options.Value.SessionKeyPrefix}:{_options.Value.SessionLockPrefix}:{typeof(TSession).Name.ToLower()}:{initKey}";
}