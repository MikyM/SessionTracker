using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using SessionTracker.Tests.Shared;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryDataProvider;

public partial class InMemoryDataProvider
{
    [Collection("InMemoryDataProvider")]
    public class RestoreAsync
    {
        [Fact]
        public async Task CorrectlyMoveSessionToRegularStore()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateEvictedKey<TestSession>(session.Key), session);
            
            // Act
            var result = await sut.RestoreAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var restored = cache.TryGetValue<TestSession>(keyCreator.CreateKey<TestSession>(session.Key), out var value);

            restored.Should().BeTrue();
            value.Should().NotBeNull();
            value.Should().BeSameAs(session);
            
            var evictedSession = cache.TryGetValue(keyCreator.CreateEvictedKey<TestSession>(session.Key), out _);
            evictedSession.Should().BeFalse();
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, _, _, _) = Helpers.GetDataSut();
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
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateKey<TestSession>(session.Key), options);
            
            // Act
            var result = await sut.RestoreAsync<TestSession>(session.Key, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyRestoredError>();
        }
    }
}