using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using Testcontainers.Redis;

namespace SessionTracker.Redis.Tests.Integration.SingleInstance;

public class SingleInstanceRedisFixture : RedisFixture
{
    private bool _initialized;
    private ConnectionMultiplexer? _multiplexer;
    private RedisContainer? _redisContainer;
    private IServiceProvider? _serviceProvider;
    
    private readonly string _redisContainerName = "session-tracker-redis-" + Guid.NewGuid();
    private readonly int _port = Random.Shared.Next(6000, 17000);

    public SingleInstanceRedisFixture()
    {
        InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
    
    public sealed override async Task InitializeAsync()
    {
        if (_initialized)
            return;
        
        Console.WriteLine(
            $"[redis-explorer-tests {TimeProvider.System.GetUtcNow().DateTime.ToString(CultureInfo.InvariantCulture)}] Creating Redis container...");

        _redisContainer = new RedisBuilder()
            .WithImage(RedisImage)
            .WithName(_redisContainerName)
            .WithPortBinding(_port, _port)
            .Build();
        
        await _redisContainer.StartAsync();

        _multiplexer = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());

        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(_multiplexer);

        var set = new RedisSessionTrackerSettings();
        var opt = Options.Create(set);

        services.AddSingleton(opt);

        services.AddSingleton<RedisSessionTrackerKeyCreator>();

        services.AddLogging();

        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<RedisSessionDataProvider>();

        services.AddSingleton<RedisSessionLockProvider>();
        
        services.AddSingleton<IDatabase>(x => x.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

        var redLockMultiplexer = (RedLockMultiplexer)_multiplexer;
        
        redLockMultiplexer.RedisKeyFormat = set.SessionKeyPrefix + ":" + set.SessionLockPrefix + ":{0}";
        
        var factory = RedLockFactory.Create(new List<RedLockMultiplexer> { redLockMultiplexer });

        services.AddSingleton<IDistributedLockFactory>(factory);
        
        _serviceProvider = services.BuildServiceProvider();

        Console.WriteLine(
            $"[redis-explorer-tests {TimeProvider.System.GetUtcNow().DateTime.ToString(CultureInfo.InvariantCulture)}] Redis container created");

        _initialized = true;
    }

    public override async Task TeardownAsync()
    {
        if (_multiplexer != null)
        {
            await _multiplexer.DisposeAsync();
        }

        if (_redisContainer is not null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }

        Console.WriteLine(
            $"[redis-explorer-tests {TimeProvider.System.GetUtcNow().DateTime.ToString(CultureInfo.InvariantCulture)}] Test containers pruned");
    }

    public override (RedisSessionDataProvider Sut, IDatabase Cache, RedisSessionTrackerKeyCreator KeyCreator) GetSut(
        TimeProvider? timeProvider = null)
        => _serviceProvider is null
            ? throw new InvalidOperationException("Fixture has not been initialized")
            : new(new RedisSessionDataProvider(
                    _serviceProvider.GetRequiredService<IOptions<RedisSessionTrackerSettings>>(),
                    _serviceProvider.GetRequiredService<IConnectionMultiplexer>(),
                    _serviceProvider.GetRequiredService<ILogger<RedisSessionDataProvider>>(),
                    _serviceProvider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>(),
                    _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>(),
                    timeProvider ?? _serviceProvider.GetRequiredService<TimeProvider>()),
                _serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase(),
                _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>());
    
    public override (RedisSessionLockProvider Sut, IDatabase Cache, RedisSessionTrackerKeyCreator KeyCreator) GetLockSut(
        TimeProvider? timeProvider = null)
        => _serviceProvider is null
            ? throw new InvalidOperationException("Fixture has not been initialized")
            : new(new RedisSessionLockProvider(
                    _serviceProvider.GetRequiredService<IDistributedLockFactory>(),
                    _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>(),
                    timeProvider ?? _serviceProvider.GetRequiredService<TimeProvider>()),
                _serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase(),
                _serviceProvider.GetRequiredService<RedisSessionTrackerKeyCreator>());
}