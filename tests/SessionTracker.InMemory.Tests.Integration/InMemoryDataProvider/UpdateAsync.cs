using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Remora.Results;
using SessionTracker.Tests.Shared;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryDataProvider;

public partial class InMemoryDataProvider
{
    [Collection("InMemoryDataProvider")]
    public class UpdateAsync
    {
        [Fact]
        public async Task CorrectlyUpdateItemInDataStoreUsingExisting()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateKey<TestSession>(session.Key), session);

            var old = session.Description;
            var newD = "a";
            
            // Act
            session.Description = newD;
            var result = await sut.UpdateAsync(session);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var existing = cache.TryGetValue<TestSession>(keyCreator.CreateKey<TestSession>(session.Key), out var value);

            existing.Should().BeTrue();
            value.Description.Should().Be(newD);
        }
        
        [Fact]
        public async Task CorrectlyUpdateItemInDataStoreUsingFake()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateKey<TestSession>(session.Key), session);

            var old = session.Description;
            var fake = SharedHelpers.CreateSession(session.Key);
            fake.Description = "x";
            
            // Act
            var result = await sut.UpdateAsync(fake);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var existing = cache.TryGetValue<TestSession>(keyCreator.CreateKey<TestSession>(session.Key), out var value);

            existing.Should().BeTrue();
            value.Description.Should().Be(fake.Description);
        }
        
        [Fact]
        public async Task ReturnSessionAlreadyEvictedErrorWhenFoundInEvictionCache()
        {
            // Arrange
            var (sut, _, cache, keyCreator) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            cache.Set(keyCreator.CreateEvictedKey<TestSession>(session.Key), session);
            
            // Act
            var result = await sut.UpdateAsync(session);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, _, _, _) = Helpers.GetDataSut();
            var session = SharedHelpers.CreateSession();
            var options = new SessionEntryOptions();
            
            // Act
            var result = await sut.UpdateAsync(session);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<NotFoundError>();
        }
    }
}