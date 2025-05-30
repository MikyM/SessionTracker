using SessionTracker.Redis.Tests.Integration.Extensions;

namespace SessionTracker.Redis.Tests.Integration.RedisDataProvider;

public abstract partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
    public abstract class RestoreAsyncShould<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : RedisFixture
    {
        [Fact]
        public async Task CorrectlyMoveSessionToRegularStore()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            await cache.SetTestSessionAsync(keyCreator.CreateEvictedKey<TestSession>(session.Key), session);
            
            // Act
            var result = await sut.RestoreAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var restored = await cache.HashGetAllAsync(keyCreator.CreateKey<TestSession>(session.Key));

            restored.Should().NotBeNull();
            restored.Should().HaveCount(3);
            
            var evicted = await cache.HashGetAllAsync(keyCreator.CreateEvictedKey<TestSession>(session.Key));
            evicted.Should().NotBeNull();
            evicted.Should().BeEmpty();
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, cache, _) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            var options = new SessionEntryOptions();
            
            // Act
            var result = await sut.RestoreAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<NotFoundError>();
        }
        
        [Fact]
        public async Task ReturnSessionAlreadyRestoredWhenFoundInRegularCache()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            await cache.SetTestSessionAsync(keyCreator.CreateKey<TestSession>(session.Key), session);
            
            // Act
            var result = await sut.RestoreAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyRestoredError>();
        }
    }
}