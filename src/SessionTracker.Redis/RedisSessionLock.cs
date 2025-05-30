//
//  RedisSessionLock.cs
//


using SessionTracker.Abstractions;

namespace SessionTracker.Redis;

/// <summary>
/// A redis based implementation of <see cref="ISessionLock"/>.
/// </summary>
[PublicAPI]
public sealed class RedisSessionLock : ISessionLock, IEquatable<RedisSessionLock>
{
    internal RedisSessionLock(IRedLock redLock, DateTimeOffset expiresAt)
    {
        _redLock = redLock;
        ExpiresAt = expiresAt;
    }

    /// <inheritdoc />
    public DateTimeOffset ExpiresAt { get; }

    /// <inheritdoc />
    public string Resource => _redLock.Resource;

    /// <inheritdoc />
    public string Id => _redLock.LockId;

    /// <inheritdoc />
    public bool IsAcquired => _redLock.IsAcquired;

    /// <inheritdoc />
    public SessionLockStatus Status => TranslateRedLockStatus(_redLock.Status);
    
    /// <summary>
    /// Inner RedLock.
    /// </summary>
    private readonly IRedLock _redLock;

    /// <summary>
    /// Translates the status.
    /// </summary>
    /// <param name="status">Status to translate.</param>
    /// <returns>Translated status.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal static SessionLockStatus TranslateRedLockStatus(RedLockStatus status)
        => status switch
        {
            RedLockStatus.Unlocked => SessionLockStatus.Unlocked,
            RedLockStatus.Acquired => SessionLockStatus.Acquired,
            RedLockStatus.NoQuorum => SessionLockStatus.NoQuorum,
            RedLockStatus.Conflicted => SessionLockStatus.Conflicted,
            RedLockStatus.Expired => SessionLockStatus.Expired,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _redLock.DisposeAsync();

    /// <inheritdoc />
    public void Dispose()
        => _redLock.Dispose();

    /// <summary>
    /// Base implementation of the equatable.
    /// </summary>
    /// <param name="other">Other.</param>
    /// <returns>Whether two locks are equal.</returns>
    public bool Equals(RedisSessionLock? other)
    {
        if (other is null)
            return false;

        return Resource == other.Resource && Id == other.Id;
    }

    /// <summary>
    /// Base implementation of the equatable.
    /// </summary>
    /// <param name="obj">Other.</param>
    /// <returns>Whether two locks are equal.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((RedisSessionLock)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Resource, Id);
    }

    /// <summary>
    /// Compares two locks.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(RedisSessionLock? left, RedisSessionLock? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two locks.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(RedisSessionLock? left, RedisSessionLock? right)
    {
        return !Equals(left, right);
    }
}
