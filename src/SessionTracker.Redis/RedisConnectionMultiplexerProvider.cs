using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Options;
using SessionTracker.Redis.Abstractions;

namespace SessionTracker.Redis;

/// <inheritdoc cref="IRedisConnectionMultiplexerProvider"/>
/// <inheritdoc cref="IAsyncDisposable"/>
/// <inheritdoc cref="IDisposable"/>
[UsedImplicitly]
public sealed class RedisConnectionMultiplexerProvider : IRedisConnectionMultiplexerProvider, IDisposable, IAsyncDisposable
{
    private readonly IOptions<RedisSessionTrackerSettings> _options;
    
    private readonly SemaphoreSlim _connectionLock = new(1,1);
    
    private volatile IConnectionMultiplexer? _connectionMultiplexer;
    
    private volatile ConfigurationOptions? _seRedisConfigurationOptions;
    
    private readonly bool _shouldDisposeConnectionMultiplexer;

    /// <summary>
    /// Creates a new instance of <see cref="RedisConnectionMultiplexerProvider"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="configuration">Configuration.</param>
    /// <param name="multiplexer">Multiplexer if any.</param>
    internal RedisConnectionMultiplexerProvider(IOptions<RedisSessionTrackerSettings> options, ConfigurationOptions configuration, IConnectionMultiplexer? multiplexer = null)
    {
        _options = options;
        _connectionMultiplexer = multiplexer;
        
        _shouldDisposeConnectionMultiplexer = multiplexer is null;
        
        _seRedisConfigurationOptions = configuration;
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="RedisConnectionMultiplexerProvider"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="multiplexer">Multiplexer if any.</param>
    public RedisConnectionMultiplexerProvider(IOptions<RedisSessionTrackerSettings> options, IConnectionMultiplexer? multiplexer = null)
    {
        _options = options;
        _connectionMultiplexer = multiplexer;
        
        _shouldDisposeConnectionMultiplexer = multiplexer is null;
    }
    
    private readonly PropertyInfo _configurationOptionsGetter =
        typeof(ConnectionMultiplexer).GetProperty("RawConfig", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not find RawConfig property on ConnectionMultiplexer type.");

    private ConfigurationOptions GetOptions(IConnectionMultiplexer multiplexer)
        => ((ConfigurationOptions)_configurationOptionsGetter.GetValue((ConnectionMultiplexer)multiplexer)!).Clone();

    /// <inheritdoc/>
    public async ValueTask<IConnectionMultiplexer> GetConnectionMultiplexerAsync()
    {
        await ConnectAsync();
        
        return _connectionMultiplexer;
    }

    /// <inheritdoc/>
    public async ValueTask<ConfigurationOptions> GetConfigurationOptionsAsync()
    {
        var config = _seRedisConfigurationOptions;
        
        if (config is not null)
        {
            return config;
        }
        
        await ConnectAsync();
        
        var options = GetOptions(_connectionMultiplexer);
        
        Interlocked.CompareExchange(ref _seRedisConfigurationOptions, options, null);
        
        return _seRedisConfigurationOptions;
    }

    [MemberNotNull(nameof(_connectionMultiplexer))]
    private ValueTask ConnectAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        var multiplexer = _connectionMultiplexer;
        
        if (multiplexer is not null)
        {
            Debug.Assert(_connectionMultiplexer is not null);
            
            return ValueTask.CompletedTask;
        }
        
        return ConnectSlowAsync(token);
    }
    
     [MemberNotNull(nameof(_connectionMultiplexer))]
    private async ValueTask ConnectSlowAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        
        await _connectionLock.WaitAsync(token);
        
        try
        {
            // check again in case some process finished connecting prior to us waiting on the lock
            var multiplexer = _connectionMultiplexer;
            
            if (multiplexer is not null)
            {
                Debug.Assert(_connectionMultiplexer is not null);
                
                return;
            }
            
            IConnectionMultiplexer? connection = null;
                
            if (_options.Value.RedisConfigurationOptions is not null)
            {
                connection = await ConnectionMultiplexer.ConnectAsync(_options.Value.RedisConfigurationOptions);
            }
            else if (_options.Value.MultiplexerFactory is not null)
            {
                connection = await _options.Value.MultiplexerFactory();
            }

            if (connection is null)
            {
                throw new InvalidOperationException("No connection option for the multiplexer was configured and no IConnectionMultiplexer was found registered in DI");
            }

            PrepareConnection(connection);

            multiplexer = connection;
                
            _ = Interlocked.Exchange(ref _connectionMultiplexer, multiplexer);
            
            Debug.Assert(_connectionMultiplexer is not null);
        }
        finally
        {
            _connectionLock.Release();
        }
    }
    
    private void PrepareConnection(IConnectionMultiplexer connection)
    {
        ValidateServerFeatures(connection);
        TryRegisterProfiler(connection);
    }

    private static void ValidateServerFeatures(IConnectionMultiplexer connection)
    {
        _ = connection ?? throw new InvalidOperationException($"{nameof(connection)} cannot be null.");
    }

    private void TryRegisterProfiler(IConnectionMultiplexer connection)
    {
        _ = connection ?? throw new InvalidOperationException($"{nameof(connection)} cannot be null.");

        if (_options.Value.ProfilingSession is not null)
        {
            connection.RegisterProfiler(_options.Value.ProfilingSession);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connectionLock.Dispose();

        if (_connectionMultiplexer != null && _shouldDisposeConnectionMultiplexer)
        {
            _connectionMultiplexer.Dispose();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _connectionLock.Dispose();
        
        if (_connectionMultiplexer != null && _shouldDisposeConnectionMultiplexer)
        {
            await _connectionMultiplexer.DisposeAsync();
        }
    }
}