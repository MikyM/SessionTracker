//
//  Errors.cs
//


using Remora.Results;
using SessionTracker.Abstractions;

namespace SessionTracker;

/// <summary>
/// Error that occurs when attempting to start a session when there is an existing non-finished session associated with the given key.
/// </summary>
[PublicAPI]
public record SessionInProgressError(ISession Session) : ResultError("There is a session cached with the same key that is in progress. See Session for details.");

/// <summary>
/// Error that occurs when attempting to update a session when the session associated with the given key is already evicted.
/// </summary>
[PublicAPI]
public record SessionAlreadyEvictedError() : ResultError("Session associated with the given key has been evicted.");

/// <summary>
/// Error that occurs when attempting to restore a session when the session associated with the given key is already restored.
/// </summary>
[PublicAPI]
public record SessionAlreadyRestoredError() : ResultError("Session associated with the given key has already been restored.");

/// <summary>
/// Error that occurs when a lock for a Session couldn't be acquired.
/// </summary>
[PublicAPI]
public record SessionLockNotAcquiredError(SessionLockStatus Status) : ResultError("Couldn't obtain a lock for the given session. Check inner lock entry for details.");

