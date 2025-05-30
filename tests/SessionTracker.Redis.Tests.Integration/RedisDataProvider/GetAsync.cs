using SessionTracker.Redis.Tests.Integration.Extensions;

namespace SessionTracker.Redis.Tests.Integration.RedisDataProvider;

public abstract partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
    public abstract class GetAsyncShould<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : RedisFixture
    {
        [Fact]
        public async Task ReturnExistingSession()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            
            await cache.SetTestSessionAsync(keyCreator.CreateKey<TestSession>(session.Key),session);
            
            // Act
            var result = await sut.GetAsync<TestSession>(session.Key);
            
            // Assert
            result.IsDefined().Should().BeTrue();
            result.Entity.ToString().Should().Be(session.ToString());
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            
            // Act
            var result = await sut.GetAsync<TestSession>(session.Key);
            
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
            
            await cache.SetTestSessionAsync(keyCreator.CreateEvictedKey<TestSession>(session.Key),session);
            
            // Act
            var result = await sut.GetAsync<TestSession>(session.Key);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
    }
}