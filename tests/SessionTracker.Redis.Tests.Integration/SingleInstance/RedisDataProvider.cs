namespace SessionTracker.Redis.Tests.Integration.SingleInstance;

[CollectionDefinition("SingleInstance-RedisDataProvider")]
public class RedisDataProvider
{
    [Collection("SingleInstance-RedisDataProvider")]
    public class AddAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.AddAsyncShould<SingleInstanceRedisFixture>(fixture);

    [Collection("SingleInstance-RedisDataProvider")]
    public class GetAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.GetAsyncShould<SingleInstanceRedisFixture>(fixture);

    [Collection("SingleInstance-RedisDataProvider")]
    public class GetEvictedAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.GetEvictedAsyncShould<SingleInstanceRedisFixture>(fixture);

    [Collection("SingleInstance-RedisDataProvider")]
    public class EvictAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.EvictAsyncShould<SingleInstanceRedisFixture>(fixture);

    [Collection("SingleInstance-RedisDataProvider")]
    public class EvictAndGetAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.EvictAndGetAsyncShould<SingleInstanceRedisFixture>(fixture);
    
    [Collection("SingleInstance-RedisDataProvider")]
    public class RefreshAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.RefreshAsyncShould<SingleInstanceRedisFixture>(fixture);
    
    [Collection("SingleInstance-RedisDataProvider")]
    public class RestoreAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.RestoreAsyncShould<SingleInstanceRedisFixture>(fixture);
    
    [Collection("SingleInstance-RedisDataProvider")]
    public class RestoreAndGetAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.RestoreAndGetAsyncShould<SingleInstanceRedisFixture>(fixture);
    
    [Collection("SingleInstance-RedisDataProvider")]
    public class UpdateAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.UpdateAsyncShould<SingleInstanceRedisFixture>(fixture);
    
    [Collection("SingleInstance-RedisDataProvider")]
    public class UpdateAndGetAsyncShould(SingleInstanceRedisFixture fixture)
        : Tests.Integration.RedisDataProvider.RedisDataProvider.UpdateAndGetAsyncShould<SingleInstanceRedisFixture>(fixture);
}