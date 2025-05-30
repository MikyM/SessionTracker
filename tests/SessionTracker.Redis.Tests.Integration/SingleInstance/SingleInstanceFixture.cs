using System.Globalization;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionTracker.Redis.Abstractions;
using Testcontainers.Redis;

namespace SessionTracker.Redis.Tests.Integration.SingleInstance;

[UsedImplicitly]
public class SingleInstanceRedisFixture : RedisFixture
{
    private bool _initialized;
    private RedisContainer? _redisContainer;
    private IServiceProvider? _serviceProvider;
    
    private readonly SemaphoreSlim _initLock = new(1, 1);
    
    private readonly string _redisContainerName = "session-tracker-redis-" + Guid.NewGuid();
    private readonly int _port = Random.Shared.Next(6000, 17000);

    public SingleInstanceRedisFixture()
    {
        InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
    
    public sealed override async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        
        try
        {
            if (_initialized)
            {
                return;
            }

            Console.WriteLine(
                $"[redis-explorer-tests {TimeProvider.System.GetUtcNow().DateTime.ToString(CultureInfo.InvariantCulture)}] Creating Redis container...");

            _redisContainer = new RedisBuilder()
                .WithImage(RedisImage)
                .WithName(_redisContainerName)
                .WithPortBinding(_port, _port)
                .Build();
        
            await _redisContainer.StartAsync();

            var services = new ServiceCollection();

            services.AddSessionTracker()
                .AddRedisProviders(x =>
                {
                    x.MultiplexerFactory = async () => await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString(), c =>
                    {
                        c.AbortOnConnectFail = true;
                        c.ConnectRetry = 3;
                        c.ConnectTimeout = 5000;
                        c.AsyncTimeout = 5000;
                        c.SyncTimeout = 5000;
                    });
                });
        
            _serviceProvider = services.BuildServiceProvider();

            // force the connection prior to test start
            _serviceProvider.GetRequiredService<IRedisConnectionMultiplexerProvider>().GetConnectionMultiplexerAsync()
                .AsTask().WaitAsync(TimeSpan.FromMinutes(1)).GetAwaiter().GetResult();
            
            _serviceProvider.GetRequiredService<IDistributedLockFactoryProvider>().GetDistributedLockFactoryAsync()
                .AsTask().WaitAsync(TimeSpan.FromMinutes(1)).GetAwaiter().GetResult();

            Console.WriteLine(
                $"[redis-explorer-tests {TimeProvider.System.GetUtcNow().DateTime.ToString(CultureInfo.InvariantCulture)}] Redis container created");

            _initialized = true;
        }
        finally
        {  
            _initLock.Release();
        }
    }

    public override async Task TeardownAsync()
    {
        if (_serviceProvider is ServiceProvider serviceProvider)
        {
            await serviceProvider.DisposeAsync();
        }

        if (_redisContainer is not null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
        
        _initLock.Dispose();

        Console.WriteLine(
            $"[redis-explorer-tests {TimeProvider.System.GetUtcNow().DateTime.ToString(CultureInfo.InvariantCulture)}] Test containers pruned");
    }

    public override (RedisSessionDataProvider Sut, IDatabase Cache, RedisSessionTrackerKeyCreator KeyCreator) GetSut(
        TimeProvider? timeProvider = null)
        => _serviceProvider is null
            ? throw new InvalidOperationException("Fixture has not been initialized")
            : new(new RedisSessionDataProvider(
                    _serviceProvider.GetRequiredService<IOptions<RedisSessionTrackerSettings>>(),
                    _serviceProvider.GetRequiredService<IRedisConnectionMultiplexerProvider>(),
                    _serviceProvider.GetRequiredService<ILogger<RedisSessionDataProvider>>(),
                    _serviceProvider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>(),
                    _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>(),
                    timeProvider ?? _serviceProvider.GetRequiredService<TimeProvider>()),
                _serviceProvider.GetRequiredService<IRedisConnectionMultiplexerProvider>().GetConnectionMultiplexerAsync().AsTask().Result.GetDatabase(),
                _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>());
    
    public override (RedisSessionLockProvider Sut, IDatabase Cache, RedisSessionTrackerKeyCreator KeyCreator) GetLockSut(
        TimeProvider? timeProvider = null)
        => _serviceProvider is null
            ? throw new InvalidOperationException("Fixture has not been initialized")
            : new(new RedisSessionLockProvider(
                    _serviceProvider.GetRequiredService<IDistributedLockFactoryProvider>(),
                    _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>(),
                    timeProvider ?? _serviceProvider.GetRequiredService<TimeProvider>()),
                _serviceProvider.GetRequiredService<IRedisConnectionMultiplexerProvider>().GetConnectionMultiplexerAsync().AsTask().Result.GetDatabase(),
                _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>());
}