using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using SessionTracker.Tests.Shared;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryDataProvider;

public partial class InMemoryDataProvider
{
    [Collection("InMemoryDataProvider")]
    public class GetAsync
    {
        [Fact]
        public async Task ReturnExistingSession()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateKey<TestSession>(session.Key), session);
            
            // Act
            var result = await sut.GetAsync<TestSession>(session.Key);
            
            // Assert
            result.IsDefined().Should().BeTrue();
            result.Entity.Should().BeSameAs(session);
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, _, _, _) = Helpers.GetDataSut();
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
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateEvictedKey<TestSession>(session.Key), options);
            
            // Act
            var result = await sut.GetAsync<TestSession>(session.Key);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
    }
}