using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using SessionTracker.Redis.Abstractions;

namespace SessionTracker.Redis;

/// <inheritdoc cref="IRedisConnectionMultiplexerProvider"/>
/// <inheritdoc cref="IAsyncDisposable"/>
/// <inheritdoc cref="IDisposable"/>
[UsedImplicitly]
public sealed class DistributedLockFactoryProvider : IDistributedLockFactoryProvider, IDisposable, IAsyncDisposable
{
    private readonly IOptions<RedisSessionTrackerSettings> _options;
    
    private readonly SemaphoreSlim _connectionLock = new(1,1);

    private readonly IRedisConnectionMultiplexerProvider _multiplexerProvider;
    
    private volatile IDistributedLockFactory? _lockFactory;
    
    private readonly bool _shouldDisposeLockFactory;

    /// <summary>
    /// Creates a new instance of <see cref="RedisConnectionMultiplexerProvider"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="multiplexerProvider">Multiplexer provider.</param>
    /// <param name="lockFactory">Lock factory if any.</param>
    public DistributedLockFactoryProvider(IOptions<RedisSessionTrackerSettings> options, IRedisConnectionMultiplexerProvider multiplexerProvider, IDistributedLockFactory? lockFactory = null)
    {
        _options = options;
        _multiplexerProvider = multiplexerProvider;
        _lockFactory = lockFactory;
        
        _shouldDisposeLockFactory = lockFactory is null;
    }

    /// <inheritdoc/>
    public async ValueTask<IDistributedLockFactory> GetDistributedLockFactoryAsync(CancellationToken token = default)
    {
        await ConnectAsync(token);
        
        return _lockFactory;
    }
    
    [MemberNotNull(nameof(_lockFactory))]
    private ValueTask ConnectAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        var multiplexer = _lockFactory;
        
        if (multiplexer is not null)
        {
            Debug.Assert(_lockFactory is not null);
            
            return ValueTask.CompletedTask;
        }
        
        return ConnectSlowAsync(token);
    }
    
    [MemberNotNull(nameof(_lockFactory))]
    private async ValueTask ConnectSlowAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        
        await _connectionLock.WaitAsync(token);
        
        try
        {
            // check again in case some process finished connecting prior to us waiting on the lock
            var lockFactory = _lockFactory;
            
            if (lockFactory is not null)
            {
                Debug.Assert(_lockFactory is not null);
                
                return;
            }
            
            var multiplexer = await _multiplexerProvider.GetConnectionMultiplexerAsync(token);

            var redLockMultiplexer = new RedLockMultiplexer(multiplexer)
            {
                RedisKeyFormat = _options.Value.SessionKeyPrefix + ":" + _options.Value.SessionLockPrefix + ":{0}"
            };

            lockFactory = RedLockFactory.Create(new List<RedLockMultiplexer> { redLockMultiplexer });
                
            _ = Interlocked.Exchange(ref _lockFactory, lockFactory);
            
            Debug.Assert(_lockFactory is not null);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connectionLock.Dispose();

        if (_lockFactory != null && _shouldDisposeLockFactory && _lockFactory is RedLockFactory redLockFactory)
        {
            redLockFactory.Dispose();
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _connectionLock.Dispose();
        
        if (_lockFactory != null && _shouldDisposeLockFactory && _lockFactory is RedLockFactory redLockFactory)
        {
            redLockFactory.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}