using SessionTracker.Redis.Tests.Integration.Extensions;

namespace SessionTracker.Redis.Tests.Integration.RedisDataProvider;

public abstract partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
    public abstract class EvictAsyncShould<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : RedisFixture
    {
        [Fact]
        public async Task CorrectlyMoveSessionToEvictedStore()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            await cache.SetTestSessionAsync(keyCreator.CreateKey<TestSession>(session.Key),session);
            
            // Act
            var result = await sut.EvictAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var existingEvictedHashes = await cache.HashGetAllAsync(keyCreator.CreateEvictedKey<TestSession>(session.Key));

            existingEvictedHashes.Should().NotBeNull();
            existingEvictedHashes.Should().HaveCount(3);
            existingEvictedHashes.Should().Contain(x => x.Name == "data");
            existingEvictedHashes.Should().Contain(x => x.Name == "absexp");
            existingEvictedHashes.Should().Contain(x => x.Name == "sldexp");
            
            existingEvictedHashes.First(x => x.Name == "data").Value.Should().Be(session.ToString());
            
            var nonEvicted = await cache.HashGetAllAsync(keyCreator.CreateKey<TestSession>(session.Key));
            nonEvicted.Should().BeEmpty();
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            var options = new SessionEntryOptions();
            
            // Act
            var result = await sut.EvictAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<NotFoundError>();
        }
        
        [Fact]
        public async Task ReturnSessionAlreadyEvictedWhenFoundInEvictionCache()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            await cache.SetTestSessionAsync(keyCreator.CreateEvictedKey<TestSession>(session.Key),session);
            
            // Act
            var result = await sut.EvictAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
    }
}