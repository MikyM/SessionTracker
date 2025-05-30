namespace SessionTracker.Redis.Tests.Integration;

public abstract class RedisFixture : IDisposable, IAsyncDisposable
{
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