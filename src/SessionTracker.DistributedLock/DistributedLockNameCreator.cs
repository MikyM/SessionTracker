using JetBrains.Annotations;

namespace SessionTracker.DistributedLock;

/// <summary>
/// Creates names for locks.
/// </summary>
[PublicAPI]
public class DistributedLockNameCreator
{
    /// <summary>
    /// Creates a name for a lock.
    /// </summary>
    /// <param name="initKey">The initial key.</param>
    /// <typeparam name="TSession">The type of the session.</typeparam>
    /// <returns>The created lock name</returns>
    public string CreateName<TSession>(string initKey) where TSession : Session
        => $"session-tracker:lock:{typeof(TSession).Name.ToLower()}:{initKey}";
}