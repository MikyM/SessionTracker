using System.Diagnostics;

namespace SessionTracker.Redis.Tests.Integration.RedisLockProvider;

[CollectionDefinition("RedisLockProvider")]
public abstract partial class RedisLockProvider
{
    [Collection("RedisLockProvider")]
    public abstract class AcquireAsyncShould<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : RedisFixture
    {
        [Fact]
        public async Task AcquireLockWithCorrectData()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var lockedResourceKey = CreateLockKey(keyCreator, session);

            // Act
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp);

            // Assert

            result.IsDefined().Should().BeTrue();
            
            var actual = await cache.StringGetAsync(lockedResourceKey);

            actual.Should().NotBeNull();
            actual.HasValue.Should().BeTrue();
            actual.ToString().Should().Be(result.Entity.Id);
            
            result.IsDefined().Should().BeTrue();
            result.Entity.IsAcquired.Should().BeTrue();
            result.Entity.Status.Should().Be(SessionLockStatus.Acquired);
            result.Entity.Resource.Should().Be(keyCreator.CreateLockKey<TestSession>(session.Key));
        }
        
        [Fact]
        public async Task FailToAcquireLockWhenWaitTimeExpires()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var waitTime = TimeSpan.FromSeconds(2);
            var retryTime = TimeSpan.FromMilliseconds(200);
            
            var lockedResourceKey = CreateLockKey(keyCreator, session);
            await cache.StringSetAsync(lockedResourceKey, string.Empty);

            // Act

            var st = new Stopwatch();
            
            st.Start();
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp, waitTime, retryTime);
            st.Stop();

            // Assert
            
            result.IsDefined().Should().BeFalse();
            result.Error.Should().BeOfType<SessionLockNotAcquiredError>();
            result.Error.As<SessionLockNotAcquiredError>().Status.Should().Be(SessionLockStatus.Conflicted);
            st.Elapsed.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(250));
        }
        
        [Fact]
        public async Task FailToAcquireLockWhenCtCancelled()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var waitTime = TimeSpan.FromSeconds(2);
            var retryTime = TimeSpan.FromMilliseconds(200);
            var cts = new CancellationTokenSource();
            var cancelAfter = TimeSpan.FromSeconds(1);
            
            var lockedResourceKey = CreateLockKey(keyCreator, session);
            await cache.StringSetAsync(lockedResourceKey, string.Empty);

            // Act

            var st = new Stopwatch();
            
            st.Start();
            _ = Task.Run(async () =>
            {
                await Task.Delay(cancelAfter, CancellationToken.None);
                await cts.CancelAsync();
            }, CancellationToken.None);
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp, waitTime, retryTime, cts.Token);
            st.Stop();

            // Assert
            
            result.IsDefined().Should().BeFalse();
            result.Error.Should().BeOfType<ExceptionError>();
            result.Error.As<ExceptionError>().Exception.Should().BeOfType<TaskCanceledException>();
            st.Elapsed.Should().BeCloseTo(cancelAfter, TimeSpan.FromMilliseconds(200));
        }
        
        [Fact]
        public async Task AcquireLockAfterRetrying()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var waitTime = TimeSpan.FromSeconds(5);
            var retryTime = TimeSpan.FromMilliseconds(200);
            var unlockAfter = TimeSpan.FromSeconds(3);
            
            var lockedResourceKey = CreateLockKey(keyCreator, session);
            await cache.StringSetAsync(lockedResourceKey, string.Empty);

            // Act

            var st = new Stopwatch();
            
            st.Start();
            _ = Task.Run(async () =>
            {
                await Task.Delay(unlockAfter);
                await cache.KeyDeleteAsync(lockedResourceKey);
            });
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp, waitTime, retryTime);
            st.Stop();

            // Assert
            
            result.IsDefined().Should().BeTrue();
            result.Entity.IsAcquired.Should().BeTrue();
            result.Entity.Status.Should().Be(SessionLockStatus.Acquired);
            st.Elapsed.Should().BeCloseTo(unlockAfter, TimeSpan.FromMilliseconds(300));
        }
        
        private static string CreateLockKey(RedisSessionTrackerKeyCreator creator, TestSession session)
            => "session-tracker:lock:" + creator.CreateLockKey<TestSession>(session.Key);
    }
}
