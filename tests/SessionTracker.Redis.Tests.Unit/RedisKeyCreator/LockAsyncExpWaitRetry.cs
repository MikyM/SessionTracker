using RedLockNet;
using SessionTracker.Abstractions;

namespace SessionTracker.Redis.Tests.Unit.RedisKeyCreator;

public partial class RedisKeyCreator
{
    [Collection("RedisKeyCreator")]
    public class LockAsyncExpWaitRetryShould
    {
        private readonly RedisSessionTrackerDataProviderTestsFixture _fixture;

        public LockAsyncExpWaitRetryShould(RedisSessionTrackerDataProviderTestsFixture fixture)
        {
            _fixture = fixture;
        }
    }
}