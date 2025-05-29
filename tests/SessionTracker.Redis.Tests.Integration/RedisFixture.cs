namespace SessionTracker.Redis.Tests.Integration;

public abstract class RedisFixture : IDisposable, IAsyncDisposable
{
    public string TestString => "testString";
    public string GetKey() => Guid.NewGuid().ToString();
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
    public TimeSpan AcceptedDelta = TimeSpan.FromSeconds(1);
    public const string RedisImage = "redis/redis-stack:latest";
    public void Initialize() => InitializeAsync().GetAwaiter().GetResult();
    public abstract Task InitializeAsync();
    public abstract Task TeardownAsync();
    public abstract (RedisSessionDataProvider Sut, IDatabase Cache, RedisSessionTrackerKeyCreator KeyCreator) GetSut(TimeProvider? timeProvider = null);
    public abstract (RedisSessionLockProvider Sut, IDatabase Cache, RedisSessionTrackerKeyCreator KeyCreator) GetLockSut(TimeProvider? timeProvider = null);
    
    public void Dispose()
    {
        TeardownAsync().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        await TeardownAsync();
    }
}