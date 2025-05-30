using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using SessionTracker.Tests.Shared;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryDataProvider;

public partial class InMemoryDataProvider
{
    [Collection("InMemoryDataProvider")]
    public class AddAsyncShould
    {
        [Fact]
        public async Task CorrectlyInsertItemToDataStore()
        {
            // Arrange
            var (sut, _, cache, _) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            // Act
            var result = await sut.AddAsync(session, options);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var existing = cache.TryGetValue<TestSession>(session.ProviderKey, out var value);

            existing.Should().BeTrue();
            value.Should().BeSameAs(session);
        }
        
        [Theory]
        [InlineData(true)]
        public async Task SetsCorrectEntryOptions(bool relativeToNow)
        {
            // Arrange
            var (sut, _, cache, _) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var sld = TimeSpan.FromSeconds(30);
            var rel = TimeSpan.FromSeconds(50);
            var abs = DateTimeOffset.UtcNow.Add(rel);

            var options = new SessionEntryOptions()
            {
                SlidingExpiration = sld,
                AbsoluteExpiration = relativeToNow ? null : abs,
                AbsoluteExpirationRelativeToNow = relativeToNow ? rel : null
            };
            
            // Act
            var result = await sut.AddAsync(session, options);
            
            // Assert
            result.IsSuccess.Should().BeTrue();

            var data = Helpers.GetRawCacheEntries(cache);
            
            var first = data.First();
            
            first.SlidingExpiration.Should().Be(sld);

            if (relativeToNow)
            {
                first.AbsoluteExpirationRelativeToNow.Should().Be(rel);
            }
            else
            {
                first.AbsoluteExpiration.Should().Be(abs);
            }
        }
        
        [Fact]
        public async Task SetProviderKeys()
        {
            // Arrange
            var (sut, _, _, _) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            // Act
            _ = await sut.AddAsync(session, options);
            
            // Assert
            session.ProviderKey.Should().NotBeNullOrWhiteSpace();
            session.EvictedProviderKey.Should().NotBeNullOrWhiteSpace();
        }
        
        [Fact]
        public async Task ReturnSessionInProgressErrorWhenCollisionFound()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session1 = SharedHelpers.CreateSession();
            var session2 = SharedHelpers.CreateSession(session1.Key);

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateKey<TestSession>(session1.Key), session1);
            
            // Act
            var result = await sut.AddAsync(session2, options);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionInProgressError>();
            result.Error.As<SessionInProgressError>().Session.Should().BeSameAs(session1);
        }
    }
}