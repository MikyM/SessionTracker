using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using SessionTracker.Abstractions;

namespace SessionTracker.InMemory;

/// <summary>
/// An in-memory session lock.
/// </summary>
[PublicAPI]
public sealed class InMemorySessionLock : ISessionLock, IEquatable<InMemorySessionLock>
{
    private bool _disposed;
    
    private TimeSpan _expiryJitter = TimeSpan.FromMilliseconds(10);
    
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_cacheQueue is null) 
                return;
            
            var status = await _cacheQueue.Enqueue(x =>
            {
                var currentExists = x.TryGetValue<string>(Resource, out var value);

                if (currentExists && value == Id)
                {
                    x.Remove(Resource);

                    return SessionLockStatus.Unlocked;
                }

                return SessionLockStatus.Expired;
            });

            Status = status;
            IsAcquired = false;
            _cacheQueue = null;
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_cacheQueue is null) 
                return;
            
            var status = _cacheQueue.Enqueue(x =>
            {
                var currentExists = x.TryGetValue<string>(Resource, out var value);

                if (currentExists && value == Id)
                {
                    x.Remove(Resource);

                    return SessionLockStatus.Unlocked;
                }

                return SessionLockStatus.Expired;
            }).GetAwaiter().GetResult();

            Status = status;
            IsAcquired = false;
            _cacheQueue = null;
        }
        finally
        {
            _disposed = true;
        }
    }

    internal InMemorySessionLock(string resource, string id, bool isAcquired, SessionLockStatus status, MemoryCacheQueue? cacheQueue, TimeSpan expirationTime)
    {
        Resource = resource;
        Id = id;
        IsAcquired = isAcquired;
        Status = status;
        
        _cacheQueue = cacheQueue;

        if (isAcquired && status is SessionLockStatus.Acquired)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(expirationTime.Subtract(_expiryJitter));

                    if (isAcquired && status is SessionLockStatus.Acquired)
                    {
                        Status = SessionLockStatus.Expired;
                    }
                }
                catch
                {
                    // ignore
                }
            });
        }
    }
    
    private MemoryCacheQueue? _cacheQueue;

    /// <inheritdoc/>
    public string Resource { get; }
    /// <inheritdoc/>
    public string Id { get; }
    /// <inheritdoc/>
    public bool IsAcquired { get; private set; }
    /// <inheritdoc/>
    public SessionLockStatus Status { get; private set; }

    /// <inheritdoc/>
    public bool Equals(InMemorySessionLock? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Resource == other.Resource && Id == other.Id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is InMemorySessionLock other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Resource, Id);
    }
}