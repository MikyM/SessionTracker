using JetBrains.Annotations;
using Medallion.Threading;
using SessionTracker.Abstractions;

namespace SessionTracker.DistributedLock;

/// <summary>
/// An implementation of <see cref="ISessionLock"/> with <see cref="IDistributedSynchronizationHandle"/> underneath.
/// </summary>
[PublicAPI]
public sealed class DistributedLockSessionLock : ISessionLock
{
    private readonly IDistributedSynchronizationHandle _handle;

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> instance which may be used to 
    /// monitor whether the handle to the lock is lost before the handle is
    /// disposed. 
    /// 
    /// For example, this could happen if the lock is backed by a 
    /// database and the connection to the database is disrupted.
    /// 
    /// Not all lock types support this; those that don't will return <see cref="CancellationToken.None"/>
    /// which can be detected by checking <see cref="CancellationToken.CanBeCanceled"/>.
    /// 
    /// For lock types that do support this, accessing this property may incur additional
    /// costs, such as polling to detect connectivity loss.
    /// </summary>
    public CancellationToken HandleLostToken => _handle.HandleLostToken;

    internal DistributedLockSessionLock(IDistributedSynchronizationHandle handle, DateTimeOffset expiration, string resource, bool isAcquired, SessionLockStatus status, string id)
    {
        _handle = handle;
        
        ExpiresAt = expiration;
        Resource = resource;
        Id = id;
        Status = status;
        IsAcquired = isAcquired;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _handle.Dispose();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return _handle.DisposeAsync();
    }

    /// <inheritdoc/>
    public DateTimeOffset ExpiresAt { get; }
    /// <inheritdoc/>
    public string Resource { get; }
    /// <inheritdoc/>
    public string Id { get; }
    /// <inheritdoc/>
    public bool IsAcquired { get; }
    /// <inheritdoc/>
    public SessionLockStatus Status { get; }
}