using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using SessionTracker.Abstractions;
using SessionTracker.InMemory.Extensions;

namespace SessionTracker.InMemory;

/// <summary>
/// An implementation of <see cref="ISessionDataProvider"/> based on in-memory cache <see cref="IMemoryCache"/>
/// </summary>
[PublicAPI]
public class InMemorySessionDataProvider : ISessionDataProvider
{
    private readonly MemoryCacheQueue _cacheQueue;
    private readonly InMemorySessionTrackerKeyCreator _keyCreator;

    /// <summary>
    /// Creates a new instance of <see cref="InMemorySessionDataProvider"/>.
    /// </summary>
    /// <param name="cacheQueue">The underlying cache.</param>
    /// <param name="keyCreator">Key creator.</param>
    public InMemorySessionDataProvider(MemoryCacheQueue cacheQueue, InMemorySessionTrackerKeyCreator keyCreator)
    {
        _cacheQueue = cacheQueue;
        _keyCreator = keyCreator;
    }
    
    private readonly record struct OperationResult<TSession>(TSession? Session, bool? Flag)
    {
    }

    /// <inheritdoc/>
    public async Task<Result<TSession>> GetAsync<TSession>(string key,
        CancellationToken ct = default) where TSession : Session
    {
        var keys = _keyCreator.CreateKeys<TSession>(key);
        
        var result = await _cacheQueue.EnqueueAsync(memoryCache =>
        {
            var nonEvicted = memoryCache.TryGetValue<TSession>(keys.Regular, out var session);
            if (nonEvicted)
            {
                return new OperationResult<TSession>(session, null);
            }
            
            var evicted = memoryCache.TryGetValue(keys.Evicted, out _);
            
            return new OperationResult<TSession>(null, evicted);
        });

        if (result.Session is not null)
        {
            return result.Session;
        }

        if (result.Session is null && result.Flag is true)
        {
            return new SessionAlreadyEvictedError();
        }

        return new NotFoundError($"The given key \"{key}\" held no session in the cache.");
    }

    /// <inheritdoc/>
    public async Task<Result<TSession>> GetEvictedAsync<TSession>(string key,
        CancellationToken ct = default) where TSession : Session
    {
        var keys = _keyCreator.CreateKeys<TSession>(key);
        
        var result = await _cacheQueue.EnqueueAsync(memoryCache =>
        {
            var evicted = memoryCache.TryGetValue<TSession>(keys.Evicted, out var session);
            if (evicted)
            {
                return new OperationResult<TSession>(session, null);
            }
            
            var nonEvicted = memoryCache.TryGetValue(keys.Regular, out _);
            
            return new OperationResult<TSession>(null, nonEvicted);
        });

        if (result.Session is not null)
        {
            return result.Session;
        }

        if (result.Session is null && result.Flag is true)
        {
            return new SessionAlreadyRestoredError();
        }

        return new NotFoundError($"The given key \"{key}\" held no session in the cache.");
    }

    /// <inheritdoc/>
    public async Task<Result> AddAsync<TSession>(TSession session, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session
    {
        var keys = _keyCreator.CreateKeys<TSession>(session.Key);
        
        session.SetProviderKeys(keys.Regular, keys.Evicted);
        
        var result = await _cacheQueue.EnqueueAsync(memoryCache =>
        {
            var ongoingResult = memoryCache.TryGetValue<TSession>(keys.Regular, out var ongoing);
            if (ongoingResult)
            {
                return new OperationResult<TSession>(ongoing, true);
            }

            memoryCache.Set(keys.Regular, session, options.ToMemoryCacheEntryOptions());
            
            return new OperationResult<TSession>(null, true);
        });

        if (result.Session is not null && result.Flag is true)
        {
            return new SessionInProgressError(result.Session);
        }

        return Result.Success;
    }

    /// <inheritdoc/>
    public async Task<Result> RefreshAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        var keys = _keyCreator.CreateKeys<TSession>(key);
            
        var result = await _cacheQueue.EnqueueAsync(memoryCache =>
        {
            var regularResult = memoryCache.TryGetValue<TSession>(keys.Regular, out _);
            if (regularResult)
            {
                return new OperationResult<TSession>(null, true);
            }

            var evictedResult = memoryCache.TryGetValue(keys.Evicted, out _);
            
            return new OperationResult<TSession>(null, evictedResult ? false : null);
        });

        if (result.Flag is true)
        {
            return Result.Success;
        }

        if (result.Flag is false)
        {
            return new SessionAlreadyEvictedError();
        }
        
        return new NotFoundError($"The given key \"{key}\" held no session in the cache.");
    }

    /// <inheritdoc/>
    public async Task<Result> UpdateAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session
    {
        var keys = _keyCreator.CreateKeys<TSession>(session.Key);
        
        var result = await _cacheQueue.EnqueueAsync(memoryCache =>
        {
            var regularResult = memoryCache.TryGetValue<TSession>(keys.Regular, out _);

            if (regularResult)
            {
                memoryCache.Set(keys.Regular, session);
                return new OperationResult<TSession>(null, true);
            }
            
            var evictedResult = memoryCache.TryGetValue<TSession>(keys.Evicted, out _);
            
            return new OperationResult<TSession>(null, !evictedResult && !regularResult ? false : null);
        });

        if (result is { Flag: true, Session: null })
        {
            return Result.Success;
        }

        if (result.Flag is false)
        {
            return new NotFoundError($"The given key \"{session.Key}\" held no session in the cache.");
        }

        return new SessionAlreadyEvictedError();
    }

    /// <inheritdoc/>
    public async Task<Result<TSession>> UpdateAndGetAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : Session
    {
        var subResult = await UpdateAsync(session, ct);
        return subResult.IsSuccess 
            ? session 
            : Result<TSession>.FromError(subResult);
    }

    /// <inheritdoc/>
    public async Task<Result> EvictAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session
    {
        var subResult = await EvictAndGetAsync<TSession>(key, options, ct);
        return subResult.IsSuccess
            ? Result.Success
            : Result.FromError(subResult);
    }

    /// <inheritdoc/>
    public async Task<Result<TSession>> EvictAndGetAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session
    {
        var keys = _keyCreator.CreateKeys<TSession>(key);
        
        var result = await _cacheQueue.EnqueueAsync(memoryCache =>
        {
            var regularResult = memoryCache.TryGetValue<TSession>(keys.Regular, out var ongoing);

            if (regularResult)
            {
                memoryCache.Remove(keys.Regular);
                memoryCache.Set(keys.Evicted, ongoing, options.ToMemoryCacheEntryOptions());
                
                return new OperationResult<TSession>(ongoing, true);
            }
            
            var evictedResult = memoryCache.TryGetValue(keys.Evicted, out _);
            
            return new OperationResult<TSession>(null, !evictedResult && !regularResult ? false : null);
        });

        if (result is { Flag: true, Session: not null })
        {
            return result.Session;
        }

        if (result.Flag is false)
        {
            return new NotFoundError($"The given key \"{key}\" held no session in the cache.");
        }

        return new SessionAlreadyEvictedError();
    }

    /// <inheritdoc/>
    public async Task<Result> RestoreAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session
    {
        var subResult = await RestoreAndGetAsync<TSession>(key, options, ct);
        return subResult.IsSuccess 
            ? Result.Success 
            : Result.FromError(subResult);
    }

    /// <inheritdoc/>
    public async Task<Result<TSession>> RestoreAndGetAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : Session
    {
        var keys = _keyCreator.CreateKeys<TSession>(key);
        
        var result = await _cacheQueue.EnqueueAsync(memoryCache =>
        {
            var evictedResult = memoryCache.TryGetValue<TSession>(keys.Evicted, out var evicted);

            if (evictedResult)
            {
                memoryCache.Remove(keys.Evicted);
                memoryCache.Set(keys.Regular, evicted, options.ToMemoryCacheEntryOptions());
                
                return new OperationResult<TSession>(evicted, true);
            }
            
            var regularResult = memoryCache.TryGetValue(keys.Regular, out _);
            
            return new OperationResult<TSession>(null, !evictedResult && !regularResult ? false : null);
        });

        if (result is { Flag: true, Session: not null })
        {
            return result.Session;
        }

        if (result.Flag is false)
        {
            return new NotFoundError($"The given key \"{key}\" held no session in the cache.");
        }

        return new SessionAlreadyRestoredError();
    }
}