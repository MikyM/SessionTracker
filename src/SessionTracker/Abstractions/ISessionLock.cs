//
//  ISessionLock.cs
//


namespace SessionTracker.Abstractions;

/// <summary>
/// Represents a session lock.
/// </summary>
[PublicAPI]
public interface ISessionLock : IAsyncDisposable, IDisposable, IEquatable<ISessionLock>
{
    /// <summary>
    /// Gets the absolute expiration time.
    /// </summary>
    DateTimeOffset ExpiresAt { get; }
    
    /// <summary>
    /// The resource on which the lock was acquired.
    /// </summary>
    string Resource { get; }
    
    /// <summary>
    /// Locks ID.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Whether the lock was successfully acquired.
    /// </summary>
    bool IsAcquired { get; }
    
    /// <summary>
    /// The lock's status.
    /// </summary>
    SessionLockStatus Status { get; }

    /// <summary>
    /// Base implementation of the equatable.
    /// </summary>
    /// <param name="other">Other.</param>
    /// <returns>Whether two locks are equal.</returns>
    bool IEquatable<ISessionLock>.Equals(ISessionLock? other) =>
        other is not null && Resource == other.Resource && Id == other.Id;
}
