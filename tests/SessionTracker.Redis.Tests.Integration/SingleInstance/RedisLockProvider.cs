namespace SessionTracker.Redis.Tests.Integration.SingleInstance;

[CollectionDefinition("SingleInstance-RedisLockProvider")]
public class RedisLockProvider
{
    [Collection("SingleInstance-RedisLockProvider")]
    public class AcquireShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisLockProvider.RedisLockProvider.AcquireAsyncShould<SingleInstanceRedisFixture>(fixture);
}