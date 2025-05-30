using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using SessionTracker.Tests.Shared;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryDataProvider;

public partial class InMemoryDataProvider
{
    [Collection("InMemoryDataProvider")]
    public class EvictAndGetAsync
    {
        [Fact]
        public async Task CorrectlyMoveSessionToEvictedStoreAndReturnData()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateKey<TestSession>(session.Key), session);
            
            // Act
            var result = await sut.EvictAndGetAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsDefined().Should().BeTrue();
            result.Entity.Should().BeSameAs(session);
            
            var evicted = cache.TryGetValue<TestSession>(keyCreator.CreateEvictedKey<TestSession>(session.Key), out var value);

            evicted.Should().BeTrue();
            value.Should().NotBeNull();
            value.Should().BeSameAs(session);
            
            var existingSession = cache.TryGetValue(keyCreator.CreateKey<TestSession>(session.Key), out _);
            existingSession.Should().BeFalse();
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, _, _, _) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();
            var options = new SessionEntryOptions();
            
            // Act
            var result = await sut.EvictAndGetAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<NotFoundError>();
        }
        
        [Fact]
        public async Task ReturnSessionAlreadyEvictedWhenFoundInEvictionCache()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateEvictedKey<TestSession>(session.Key), options);
            
            // Act
            var result = await sut.EvictAndGetAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
    }
}