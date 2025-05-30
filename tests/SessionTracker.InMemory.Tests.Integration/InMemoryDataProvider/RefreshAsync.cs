using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using SessionTracker.InMemory.Extensions;
using SessionTracker.Tests.Shared;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryDataProvider;

public partial class InMemoryDataProvider
{
    [Collection("InMemoryDataProvider")]
    public class RefreshAsync
    {
        [Fact]
        public async Task CorrectlyRefreshTheEntry()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var sld = TimeSpan.FromSeconds(5);

            var options = new SessionEntryOptions()
            {
                SlidingExpiration = sld
            };
            
            cache.Set(keyCreator.CreateKey<TestSession>(session.Key), session, options.ToMemoryCacheEntryOptions());
            
            // Act

            await Task.Delay(3);
            
            var result = await sut.RefreshAsync<TestSession>(session.Key);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            await Task.Delay(TimeSpan.FromSeconds(3));
            
            var exists = cache.TryGetValue(keyCreator.CreateKey<TestSession>(session.Key), out var value);

            exists.Should().BeTrue();
            value.Should().NotBeNull();
            value.Should().BeSameAs(session);
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, _, _, _) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();
            var options = new SessionEntryOptions();
            
            // Act
            var result = await sut.RefreshAsync<TestSession>(session.Key);
            
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
            var result = await sut.RefreshAsync<TestSession>(session.Key);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
    }
}