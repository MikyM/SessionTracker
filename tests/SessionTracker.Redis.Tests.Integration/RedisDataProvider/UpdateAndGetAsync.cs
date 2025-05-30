using System.Text.Json;
using SessionTracker.Redis.Tests.Integration.Extensions;

namespace SessionTracker.Redis.Tests.Integration.RedisDataProvider;

public abstract partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
    public abstract class UpdateAndGetAsyncShould<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : RedisFixture
    {
        [Fact]
        public async Task CorrectlyUpdateItemInDataStoreUsingExisting()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            await cache.SetTestSessionAsync(keyCreator.CreateKey<TestSession>(session.Key), session);

            var old = session.Description;
            var newD = "a";

            // Act
            session.Description = newD;
            var result = await sut.UpdateAndGetAsync(session);

            // Assert
            result.IsDefined().Should().BeTrue();
            result.Entity.ToString().Should().Be(session.ToString());

            var existing = await cache.HashGetAllAsync(keyCreator.CreateKey<TestSession>(session.Key));

            existing.Should().NotBeNullOrEmpty();
            existing.Should().HaveCount(3);
            existing.Should().Contain(x => x.Name == "data");

            JsonSerializer.Deserialize<TestSession>(existing.First(x => x.Name == "data").Value.ToString())!.Description
                .Should().Be(newD);
        }

        [Fact]
        public async Task CorrectlyUpdateItemInDataStoreUsingFake()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            
            await cache.SetTestSessionAsync(keyCreator.CreateKey<TestSession>(session.Key), session);

            var fake = SharedHelpers.CreateSession(session.Key);
            fake.Description = "x";
            
            // Act
            var result = await sut.UpdateAndGetAsync(fake);
            
            // Assert
            result.IsDefined().Should().BeTrue();
            result.Entity.ToString().Should().Be(fake.ToString());
            
            var existing = await cache.HashGetAllAsync(keyCreator.CreateKey<TestSession>(session.Key));

            existing.Should().NotBeNullOrEmpty();
            existing.Should().HaveCount(3);
            existing.Should().Contain(x => x.Name == "data");

            JsonSerializer.Deserialize<TestSession>(existing.First(x => x.Name == "data").Value.ToString())!.Description
                .Should().Be(fake.Description);
        }
        
        [Fact]
        public async Task ReturnSessionAlreadyEvictedErrorWhenFoundInEvictionCache()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            
            await cache.SetTestSessionAsync(keyCreator.CreateEvictedKey<TestSession>(session.Key), session);
            
            // Act
            var result = await sut.UpdateAndGetAsync(session);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            
            // Act
            var result = await sut.UpdateAndGetAsync(session);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<NotFoundError>();
        }
    }
}