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
    internal InMemorySessionLock(string resource, string id, bool isAcquired, SessionLockStatus status, MemoryCacheQueue? cacheQueue, DateTimeOffset expirationTime)
    {
        Resource = resource;
        Id = id;
        IsAcquired = isAcquired;
        Status = status;
        
        _cacheQueue = cacheQueue;
    }
    
    private bool _disposed;
    
    private MemoryCacheQueue? _cacheQueue;

    /// <inheritdoc/>
    public DateTimeOffset ExpiresAt { get; }

    /// <inheritdoc/>
    public string Resource { get; }
    /// <inheritdoc/>
    public string Id { get; }
    /// <inheritdoc/>
    public bool IsAcquired { get; private set; }
    /// <inheritdoc/>
    public SessionLockStatus Status { get; private set; }

    internal void SetExpired()
    {
        IsAcquired = false;
        Status = SessionLockStatus.Expired;
    }
    
    internal void SetUnlocked()
    {
        IsAcquired = false;
        Status = SessionLockStatus.Unlocked;
    }

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
            
            await _cacheQueue.EnqueueAsync(x =>
            {
                var currentExists = x.TryGetValue<string>(Resource, out var value);

                if (currentExists && value == Id)
                {
                    x.Remove(Resource);
                }
            });
            
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
            
            _cacheQueue.EnqueueAsync(x =>
            {
                var currentExists = x.TryGetValue<string>(Resource, out var value);

                if (currentExists && value == Id)
                {
                    x.Remove(Resource);
                }
            }).GetAwaiter().GetResult();

            _cacheQueue = null;
        }
        finally
        {
            _disposed = true;
        }
    }
}