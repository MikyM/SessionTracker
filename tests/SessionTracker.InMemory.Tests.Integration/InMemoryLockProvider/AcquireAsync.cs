using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using SessionTracker.Tests.Shared;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryLockProvider;

[CollectionDefinition("InMemoryLockProvider")]
public class InMemoryLockProvider
{
    [Collection("InMemoryLockProvider")]
    public class AcquireAsyncShould
    {
        [Fact]
        public async Task CauseAutomaticLockStatusChangeUponExpirationAndEviction()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(1);
            var lockedResourceKey = keyCreator.CreateLockKey<TestSession>(session.Key);

            // Act
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp);

            // Assert
            result.IsDefined().Should().BeTrue();
            result.Entity.IsAcquired.Should().BeTrue();
            result.Entity.Status.Should().Be(SessionLockStatus.Acquired);
            result.Entity.Resource.Should().Be(lockedResourceKey);
            
            await Task.Delay(TimeSpan.FromSeconds(2));
            
            result.Entity.IsAcquired.Should().BeFalse();
            result.Entity.Status.Should().Be(SessionLockStatus.Expired);
        }
        
        [Fact]
        public async Task CauseAutomaticLockStatusChangeUponRemoval()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(5);
            var lockedResourceKey = keyCreator.CreateLockKey<TestSession>(session.Key);

            // Act
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp);

            // Assert
            result.IsDefined().Should().BeTrue();
            result.Entity.IsAcquired.Should().BeTrue();
            result.Entity.Status.Should().Be(SessionLockStatus.Acquired);
            result.Entity.Resource.Should().Be(lockedResourceKey);
            
            cache.Remove(lockedResourceKey);
            
            // allow the callback to run
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            
            result.Entity.IsAcquired.Should().BeFalse();
            result.Entity.Status.Should().Be(SessionLockStatus.Unlocked);
        }
        
        [Fact]
        public async Task AcquireLockWithCorrectData()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var lockedResourceKey = keyCreator.CreateLockKey<TestSession>(session.Key);

            // Act
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp);

            // Assert

            var actual = cache.TryGetValue(lockedResourceKey, out var value);

            actual.Should().BeTrue();
            value.Should().BeOfType<InMemorySessionLock>();
            
            result.IsDefined().Should().BeTrue();
            result.Entity.IsAcquired.Should().BeTrue();
            result.Entity.Status.Should().Be(SessionLockStatus.Acquired);
            result.Entity.Resource.Should().Be(lockedResourceKey);

            var entry = Helpers.GetRawCacheEntries(cache)[0];
            entry.AbsoluteExpirationRelativeToNow.Should().Be(exp);
            entry.PostEvictionCallbacks.Should().HaveCount(1);
        }
        
        [Fact]
        public async Task FailToAcquireLockWhenWaitTimeExpires()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var waitTime = TimeSpan.FromSeconds(2);
            var retryTime = TimeSpan.FromMilliseconds(200);
            
            var lockedResourceKey = keyCreator.CreateLockKey<TestSession>(session.Key);
            cache.Set(lockedResourceKey, new  object());

            // Act

            var st = new Stopwatch();
            
            st.Start();
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp, waitTime, retryTime);
            st.Stop();

            // Assert
            
            result.IsDefined().Should().BeFalse();
            result.Error.Should().BeOfType<SessionLockNotAcquiredError>();
            result.Error.As<SessionLockNotAcquiredError>().Status.Should().Be(SessionLockStatus.Conflicted);
            st.Elapsed.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(100));
        }
        
        [Fact]
        public async Task FailToAcquireLockWhenCtCancelled()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var waitTime = TimeSpan.FromSeconds(2);
            var retryTime = TimeSpan.FromMilliseconds(200);
            var cts = new CancellationTokenSource();
            var cancelAfter = TimeSpan.FromSeconds(1);
            
            var lockedResourceKey = keyCreator.CreateLockKey<TestSession>(session.Key);
            cache.Set(lockedResourceKey, new  object());

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
            var (sut, _, cache, keyCreator) = Helpers.GetLockSut();
            var session = SharedHelpers.CreateSession();
            var exp = TimeSpan.FromSeconds(60);
            var waitTime = TimeSpan.FromSeconds(3);
            var retryTime = TimeSpan.FromMilliseconds(200);
            var unlockAfter = TimeSpan.FromSeconds(1);
            
            var lockedResourceKey = keyCreator.CreateLockKey<TestSession>(session.Key);
            cache.Set(lockedResourceKey, new  object());

            // Act

            var st = new Stopwatch();
            
            st.Start();
            _ = Task.Run(async () =>
            {
                await Task.Delay(unlockAfter);
                cache.Remove(lockedResourceKey);
            });
            var result = await sut.AcquireAsync<TestSession>(session.Key, exp, waitTime, retryTime);
            st.Stop();

            // Assert
            
            result.IsDefined().Should().BeTrue();
            result.Entity.IsAcquired.Should().BeTrue();
            result.Entity.Status.Should().Be(SessionLockStatus.Acquired);
            st.Elapsed.Should().BeCloseTo(unlockAfter, TimeSpan.FromMilliseconds(300));
        }
    }
}
