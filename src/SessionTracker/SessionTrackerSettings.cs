//
//  SessionSettings.cs
//


namespace SessionTracker;

/// <summary>
/// Holds various settings for individual session objects.
/// </summary>
[PublicAPI]
public class SessionTrackerSettings
{
    /// <summary>
    /// Holds absolute cache expiration values for various types.
    /// </summary>  
    private readonly Dictionary<Type, TimeSpan?> _absoluteCacheExpirations = new();
    
    /// <summary>
    /// Holds lock expiry values for various types.
    /// </summary>
    private readonly Dictionary<Type, TimeSpan> _lockExpirations = new();
    
    /// <summary>
    /// Holds sliding cache expiration values for various types.
    /// </summary>
    private readonly Dictionary<Type, TimeSpan?> _slidingCacheExpirations = new();

    /// <summary>
    /// Holds absolute cache expiration values for various types when they have been evicted from the primary cache.
    /// </summary>
    private readonly Dictionary<Type, TimeSpan?> _absoluteEvictionCacheExpirations = new();

    /// <summary>
    /// Holds sliding cache expiration values for various types when they have been evicted from the primary cache.
    /// </summary>
    private readonly Dictionary<Type, TimeSpan?> _slidingEvictionCacheExpirations = new();

    /// <summary>
    /// Holds the default absolute expiration value.
    /// </summary>
    private TimeSpan? _defaultAbsoluteExpiration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Holds the default sliding expiration value.
    /// </summary>
    private TimeSpan? _defaultSlidingExpiration = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Holds the default lock expiration value.
    /// </summary>
    private TimeSpan _defaultLockExpiration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Holds the default wait value.
    /// </summary>
    private TimeSpan _defaultWait = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Holds the default retry value.
    /// </summary>
    private TimeSpan _defaultRetry = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Holds the default absolute expiration value when they have been evicted from the primary cache.
    /// </summary>
    private TimeSpan? _defaultEvictionAbsoluteExpiration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Holds the default sliding expiration value when they have been evicted from the primary cache.
    /// </summary>
    private TimeSpan? _defaultEvictionSlidingExpiration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Sets the default absolute expiration value for types.
    /// </summary>
    /// <param name="defaultAbsoluteExpiration">The default value.</param>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetDefaultAbsoluteExpiration(TimeSpan? defaultAbsoluteExpiration)
    {
        _defaultAbsoluteExpiration = defaultAbsoluteExpiration;
        return this;
    }

    /// <summary>
    /// Sets the default sliding expiration value for types.
    /// </summary>
    /// <param name="defaultSlidingExpiration">The default value.</param>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetDefaultSlidingExpiration(TimeSpan? defaultSlidingExpiration)
    {
        _defaultSlidingExpiration = defaultSlidingExpiration;
        return this;
    }
    
    /// <summary>
    /// Sets the default lock expiration value for types.
    /// </summary>
    /// <param name="defaultLockExpiration">The default value.</param>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetDefaultLockExpiration(TimeSpan defaultLockExpiration)
    {
        _defaultLockExpiration = defaultLockExpiration;
        return this;
    }

    /// <summary>
    /// Sets the default wait for lock value for types.
    /// </summary>
    /// <param name="wait">The default value.</param>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetDefaultLockWait(TimeSpan wait)
    {
        _defaultWait = wait;
        return this;
    }
    
    /// <summary>
    /// Sets the default retry for lock value for types.
    /// </summary>
    /// <param name="retry">The default value.</param>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetDefaultLockRetry(TimeSpan retry)
    {
        _defaultRetry = retry;
        return this;
    }

    /// <summary>
    /// Sets the default absolute expiration value for types when they have been evicted from the primary cache.
    /// </summary>
    /// <param name="defaultAbsoluteExpiration">The default value.</param>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetDefaultEvictionAbsoluteExpiration(TimeSpan? defaultAbsoluteExpiration)
    {
        _defaultAbsoluteExpiration = defaultAbsoluteExpiration;
        return this;
    }

    /// <summary>
    /// Sets the default sliding expiration value for types when they have been evicted from the primary cache.
    /// </summary>
    /// <param name="defaultSlidingExpiration">The default value.</param>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetDefaultEvictionSlidingExpiration(TimeSpan? defaultSlidingExpiration)
    {
        _defaultSlidingExpiration = defaultSlidingExpiration;
        return this;
    }

    /// <summary>
    /// Sets the absolute cache expiration for the given type.
    /// </summary>
    /// <remarks>
    /// This method also sets the expiration time for evicted values to the same value, provided no other expiration
    /// time has already been set.
    /// </remarks>
    /// <param name="absoluteExpiration">
    /// The absolute expiration value. If the value is null, cached values will be kept indefinitely.
    /// </param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetAbsoluteExpiration<TSession>(TimeSpan? absoluteExpiration) where TSession : Session
    {
        _absoluteCacheExpirations[typeof(TSession)] = absoluteExpiration;
        _absoluteEvictionCacheExpirations.TryAdd(typeof(TSession), absoluteExpiration);

        return this;
    }
    
    /// <summary>
    /// Sets the lock expiration value for the type.
    /// </summary>
    /// <param name="lockExpiration">
    /// The lock expiration value.
    /// </param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetLockExpiration<TSession>(TimeSpan lockExpiration) where TSession : Session
    {
        _lockExpirations[typeof(TSession)] = lockExpiration;
        return this;
    }

    /// <summary>
    /// Sets the sliding cache expiration for the given type.
    /// </summary>
    /// <remarks>
    /// This method also sets the expiration time for evicted values to the same value, provided no other expiration
    /// time has already been set.
    /// </remarks>
    /// <param name="slidingExpiration">
    /// The sliding expiration value. If the value is null, cached values will be kept indefinitely.
    /// </param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetSlidingExpiration<TSession>(TimeSpan? slidingExpiration) where TSession : Session
    {
        _slidingCacheExpirations[typeof(TSession)] = slidingExpiration;
        _slidingEvictionCacheExpirations.TryAdd(typeof(TSession), slidingExpiration);
        return this;
    }

    /// <summary>
    /// Sets the absolute cache expiration for the given type when it has been evicted from the primary cache.
    /// </summary>
    /// <param name="absoluteExpiration">
    /// The absolute expiration value. If the value is null, cached values will be kept indefinitely.
    /// </param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetEvictionAbsoluteExpiration<TSession>(TimeSpan? absoluteExpiration) where TSession : Session
    {
        _absoluteEvictionCacheExpirations[typeof(TSession)] = absoluteExpiration;
        return this;
    }

    /// <summary>
    /// Sets the sliding cache expiration for the given type when it has been evicted from the primary cache.
    /// </summary>
    /// <param name="slidingExpiration">
    /// The sliding expiration value. If the value is null, cached values will be kept indefinitely.
    /// </param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The settings.</returns>
    public SessionTrackerSettings SetEvictionSlidingExpiration<TSession>(TimeSpan? slidingExpiration) where TSession : Session
    {
        _slidingEvictionCacheExpirations[typeof(TSession)] = slidingExpiration;
        return this;
    }

    /// <summary>
    /// Gets the absolute expiration time in the cache for the given type, or a default value if one does not exist.
    /// </summary>
    /// <param name="defaultExpiration">The default expiration. Defaults to 30 seconds.</param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The absolute expiration time.</returns>
    public TimeSpan? GetAbsoluteExpirationOrDefault<TSession>(TimeSpan? defaultExpiration = null) where TSession : Session
    {
        defaultExpiration ??= _defaultAbsoluteExpiration;
        return GetAbsoluteExpirationOrDefault(typeof(TSession), defaultExpiration);
    }

    /// <summary>
    /// Gets the lock expiration time for the given type, or a default value if one does not exist.
    /// </summary>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The expiration time.</returns>
    public TimeSpan GetLockExpirationOrDefault<TSession>() where TSession : Session
        => GetLockExpirationOrDefault(typeof(TSession));
    
    /// <summary>
    /// Gets the lock expiration time for the given type, or a default value if one does not exist.
    /// </summary>
    /// <returns>The expiration time.</returns>
    public TimeSpan GetLockExpirationOrDefault(Type type)
    {
        return _lockExpirations.TryGetValue(type, out var lockExpiration)
            ? lockExpiration
            : _defaultLockExpiration;
    }

    /// <summary>
    /// Gets the absolute expiration time in the cache for the given type, or a default value if one does not exist.
    /// </summary>
    /// <param name="cachedType">The cached type.</param>
    /// <param name="defaultExpiration">The default expiration. Defaults to 30 seconds.</param>
    /// <returns>The absolute expiration time.</returns>
    public TimeSpan? GetAbsoluteExpirationOrDefault(Type cachedType, TimeSpan? defaultExpiration = null)
    {
        defaultExpiration ??= _defaultAbsoluteExpiration;
        return _absoluteCacheExpirations.TryGetValue(cachedType, out var absoluteExpiration)
            ? absoluteExpiration
            : defaultExpiration;
    }

    /// <summary>
    /// Gets the absolute expiration time in the cache for the given type when it has been evicted from the primary
    /// cache, or a default value if one does not exist.
    /// </summary>
    /// <param name="defaultExpiration">The default expiration. Defaults to 30 seconds.</param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The absolute expiration time.</returns>
    public TimeSpan? GetEvictionAbsoluteExpirationOrDefault<TSession>(TimeSpan? defaultExpiration = null) where TSession : Session
    {
        defaultExpiration ??= _defaultEvictionAbsoluteExpiration;
        return GetEvictionAbsoluteExpirationOrDefault(typeof(TSession), defaultExpiration);
    }

    /// <summary>
    /// Gets the absolute expiration time in the cache for the given type when it has been evicted from the primary
    /// cache, or a default value if one does not exist.
    /// </summary>
    /// <param name="cachedType">The cached type.</param>
    /// <param name="defaultExpiration">The default expiration. Defaults to 30 seconds.</param>
    /// <returns>The absolute expiration time.</returns>
    public TimeSpan? GetEvictionAbsoluteExpirationOrDefault(Type cachedType, TimeSpan? defaultExpiration = null)
    {
        defaultExpiration ??= _defaultEvictionAbsoluteExpiration;
        return _absoluteEvictionCacheExpirations.TryGetValue(cachedType, out var absoluteExpiration)
            ? absoluteExpiration
            : defaultExpiration;
    }

    /// <summary>
    /// Gets the sliding expiration time in the cache for the given type, or a default value if one does not exist.
    /// </summary>
    /// <param name="defaultExpiration">The default expiration. Defaults to 10 seconds.</param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The sliding expiration time.</returns>
    public TimeSpan? GetSlidingExpirationOrDefault<TSession>(TimeSpan? defaultExpiration = null) where TSession : Session
    {
        defaultExpiration ??= _defaultSlidingExpiration;
        return GetSlidingExpirationOrDefault(typeof(TSession), defaultExpiration);
    }

    /// <summary>
    /// Gets the sliding expiration time in the cache for the given type, or a default value if one does not exist.
    /// </summary>
    /// <param name="cachedType">The cached type.</param>
    /// <param name="defaultExpiration">The default expiration. Defaults to 10 seconds.</param>
    /// <returns>The sliding expiration time.</returns>
    public TimeSpan? GetSlidingExpirationOrDefault(Type cachedType, TimeSpan? defaultExpiration = null)
    {
        defaultExpiration ??= _defaultSlidingExpiration;
        return _slidingCacheExpirations.TryGetValue(cachedType, out var slidingExpiration)
            ? slidingExpiration
            : defaultExpiration;
    }

    /// <summary>
    /// Gets the sliding expiration time in the cache for the given type when it has been evicted from the primary
    /// cache, or a default value if one does not exist.
    /// </summary>
    /// <param name="defaultExpiration">The default expiration. Defaults to 10 seconds.</param>
    /// <typeparam name="TSession">The cached type.</typeparam>
    /// <returns>The sliding expiration time.</returns>
    public TimeSpan? GetEvictionSlidingExpirationOrDefault<TSession>(TimeSpan? defaultExpiration = null) where TSession : Session
    {
        defaultExpiration ??= _defaultEvictionSlidingExpiration;
        return GetEvictionSlidingExpirationOrDefault(typeof(TSession), defaultExpiration);
    }

    /// <summary>
    /// Gets the sliding expiration time in the cache for the given type when it has been evicted from the primary
    /// cache, or a default value if one does not exist.
    /// </summary>
    /// <param name="cachedType">The cached type.</param>
    /// <param name="defaultExpiration">The default expiration. Defaults to 10 seconds.</param>
    /// <returns>The sliding expiration time.</returns>
    public TimeSpan? GetEvictionSlidingExpirationOrDefault(Type cachedType, TimeSpan? defaultExpiration = null)
    {
        defaultExpiration ??= _defaultEvictionSlidingExpiration;
        return _slidingEvictionCacheExpirations.TryGetValue(cachedType, out var slidingExpiration)
            ? slidingExpiration
            : defaultExpiration;
    }

    /// <summary>
    /// Gets a set of distributed cache options, with expirations relative to now.
    /// </summary>
    /// <typeparam name="TSession">The session cache entry type.</typeparam>
    /// <returns>The entry options.</returns>
    public SessionEntryOptions GetSessionEntryOptions<TSession>() where TSession : Session
    {
        var cacheOptions = new SessionEntryOptions();

        var absoluteExpiration = GetAbsoluteExpirationOrDefault<TSession>();
        if (absoluteExpiration is not null)
        {
            cacheOptions.SetAbsoluteExpiration(absoluteExpiration.Value);
        }

        var slidingExpiration = GetSlidingExpirationOrDefault<TSession>();
        if (slidingExpiration is not null && absoluteExpiration is not null)
        {
            cacheOptions.SetSlidingExpiration(slidingExpiration.Value);
        }

        return cacheOptions;
    }
    
    /// <summary>
    /// Gets a set of distributed cache options for evicted sessions, with expirations relative to now.
    /// </summary>
    /// <typeparam name="TSession">The session cache entry type.</typeparam>
    /// <returns>The entry options.</returns>
    public SessionEntryOptions GetEvictionSessionEntryOptions<TSession>() where TSession : Session
    {
        var cacheOptions = new SessionEntryOptions();

        var absoluteExpiration = GetEvictionAbsoluteExpirationOrDefault<TSession>();
        if (absoluteExpiration is not null)
        {
            cacheOptions.SetAbsoluteExpiration(absoluteExpiration.Value);
        }

        var slidingExpiration = GetEvictionSlidingExpirationOrDefault<TSession>();
        if (slidingExpiration is not null && absoluteExpiration is not null)
        {
            cacheOptions.SetSlidingExpiration(slidingExpiration.Value);
        }

        return cacheOptions;
    }
}
