using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SessionTracker.Abstractions;

namespace SessionTracker.Redis.Unit.Tests;

[UsedImplicitly]
public class RedisSessionTrackerDataProviderTestsFixture
{
    public readonly Mock<ISessionLockProvider> LockProviderMock = new();
    public readonly Mock<IOptions<RedisSessionTrackerSettings>> SettingsMock = new();
    public readonly Mock<IOptionsMonitor<JsonSerializerOptions>> JsonSettingsMock = new();
    public readonly Mock<IConnectionMultiplexer> MultiplexerMock = new();
    public readonly Mock<IDatabase> DatabaseMock = new();

    public Session Session { get; }
    public string TestKeyEvicted => "sessions:evicted:session:test";
    public string TestKey => "sessions:session:test";

    public CancellationTokenSource Cts => new();
    public string SessionKey => "test";
    public string Serialized { get; }

    public RedisSessionTrackerDataProviderTestsFixture()
    {
        Session = new Session(SessionKey);
        Serialized = JsonSerializer.Serialize(Session);
        var opt = new RedisSessionTrackerSettings();
        SettingsMock.Setup(x => x.Value).Returns(opt);
        var jopt = new JsonSerializerOptions();
        JsonSettingsMock.Setup(x => x.Get(RedisSessionTrackerSettings.JsonSerializerName)).Returns(jopt);

        MultiplexerMock.Setup(x => x.GetDatabase(-1, null)).Returns(DatabaseMock.Object);

        Service = new(SettingsMock.Object, MultiplexerMock.Object, NullLogger<Redis.RedisDataProvider>.Instance,
            JsonSettingsMock.Object, LockProviderMock.Object);
    }

    public Redis.RedisDataProvider Service { get; }

    public void Reset()
    {
        LockProviderMock.Reset();
        DatabaseMock.Reset();
    }
}