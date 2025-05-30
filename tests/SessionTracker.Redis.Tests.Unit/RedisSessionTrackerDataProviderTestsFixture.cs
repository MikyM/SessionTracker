using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RedLockNet;
using SessionTracker.Abstractions;
using SessionTracker.Redis.Abstractions;

namespace SessionTracker.Redis.Tests.Unit;

[UsedImplicitly]
public class RedisSessionTrackerDataProviderTestsFixture
{
    public readonly Mock<ISessionLockProvider> LockProviderMock = new();
    public readonly Mock<IOptions<RedisSessionTrackerSettings>> SettingsMock = new();
    public readonly Mock<IOptionsMonitor<JsonSerializerOptions>> JsonSettingsMock = new();
    public readonly Mock<IConnectionMultiplexer> MultiplexerMock = new();
    public readonly Mock<IDatabase> DatabaseMock = new();
    public readonly Mock<IRedisConnectionMultiplexerProvider> RedisConnectionProviderMock = new();
    public readonly Mock<IRedisConnectionMultiplexerProvider> RedisConnectionOptimizedProviderMock = new();
    
    public Mock<IDistributedLockFactory> RedLockFactoryMock => new();
    public readonly RedisSessionTrackerKeyCreator KeyCreator;

    public Session Session { get; }
    public string TestKeyEvicted => "session-tracker:evicted:session:test";
    public string TestKey => "session-tracker:session:test";

    public CancellationTokenSource Cts => new();
    public string SessionKey => "test";
    public string SessionLockKey => KeyCreator.CreateLockKey<Session>(SessionKey);
    public string Serialized { get; }

    public RedisSessionTrackerDataProviderTestsFixture()
    {
        Session = new Session(SessionKey);
        Serialized = JsonSerializer.Serialize(Session);
        var opt = new RedisSessionTrackerSettings()
        {
            UseBandwidthOptimizationForProxies = false
        };
        
        SettingsMock.Setup(x => x.Value).Returns(opt);
        
        KeyCreator = new RedisSessionTrackerKeyCreator(SettingsMock.Object);
        var jopt = new JsonSerializerOptions();
        
        JsonSettingsMock.Setup(x => x.Get(RedisSessionTrackerSettings.JsonSerializerName)).Returns(jopt);

        MultiplexerMock.Setup(x => x.GetDatabase(-1, null)).Returns(DatabaseMock.Object);

        RedisConnectionProviderMock.Setup(x => x.GetConnectionMultiplexerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MultiplexerMock.Object);

        RedisConnectionProviderMock.Setup(x => x.GetConfigurationOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfigurationOptions()
            {
                Proxy = Proxy.None
            });

        DataProvider = new(SettingsMock.Object, RedisConnectionProviderMock.Object, NullLogger<RedisSessionDataProvider>.Instance,
            JsonSettingsMock.Object, KeyCreator, TimeProvider.System);
        
        var optimizedOpt = new RedisSessionTrackerSettings()
        {
            UseBandwidthOptimizationForProxies = true
        };

        var optimizedSettMock = new Mock<IOptions<RedisSessionTrackerSettings>>();
        optimizedSettMock.Setup(x => x.Value).Returns(optimizedOpt);
        
        RedisConnectionOptimizedProviderMock.Setup(x => x.GetConnectionMultiplexerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MultiplexerMock.Object);

        RedisConnectionOptimizedProviderMock.Setup(x => x.GetConfigurationOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfigurationOptions()
            {
                Proxy = Proxy.Envoyproxy
            });
        
        DataProviderOptimized = new(optimizedSettMock.Object, RedisConnectionOptimizedProviderMock.Object, NullLogger<RedisSessionDataProvider>.Instance,
            JsonSettingsMock.Object, KeyCreator, TimeProvider.System);
    }

    public RedisSessionDataProvider DataProvider { get; }
    public RedisSessionDataProvider DataProviderOptimized { get; }
    
    public RedisSessionLockProvider GetLockProvider(IDistributedLockFactory redLockFactory) => new(new DistributedLockFactoryProvider(SettingsMock.Object, RedisConnectionProviderMock.Object, redLockFactory), KeyCreator, TimeProvider.System);

    public void Reset()
    {
        LockProviderMock.Reset();
        DatabaseMock.Reset();
    }
}