using SessionTracker.Redis.Tests.Integration.Extensions;

namespace SessionTracker.Redis.Tests.Integration.RedisDataProvider;

public abstract partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
    public abstract class AddAsyncShould<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : RedisFixture
    {
        [Fact]
        public async Task CorrectlyInsertItemToDataStore()
        {
            // Arrange
            var (sut, cache, _) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            var options = new SessionEntryOptions();
            
            // Act
            var result = await sut.AddAsync(session, options);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var existingHashes = await cache.HashGetAllAsync(session.ProviderKey);

            existingHashes.Should().NotBeNull();
            existingHashes.Should().HaveCount(3);
            existingHashes.Should().Contain(x => x.Name == "data");
            existingHashes.Should().Contain(x => x.Name == "absexp");
            existingHashes.Should().Contain(x => x.Name == "sldexp");
            
            existingHashes.First(x => x.Name == "data").Value.Should().Be(session.ToString());
        }
        
        
        [Theory]
        [InlineData(true)]
        public async Task SetsCorrectEntryOptions(bool relativeToNow)
        {
            // Arrange
            var (sut, cache, _) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            var sld = TimeSpan.FromSeconds(30);
            var rel = TimeSpan.FromSeconds(50);
            var abs = DateTimeOffset.UtcNow.Add(rel);
            var absSld = DateTimeOffset.UtcNow.Add(sld);

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

            var existingHashes = await cache.HashGetAllAsync(session.ProviderKey);
            existingHashes.Should().NotBeNull();
            existingHashes.Should().HaveCount(3);
            
            var exp = await cache.KeyExpireTimeAsync(session.ProviderKey);

            exp.Should().BeCloseTo(sld > rel ? abs.ToUniversalTime().UtcDateTime : absSld.ToUniversalTime().UtcDateTime, TimeSpan.FromSeconds(1));

            var sliding = existingHashes.First(x => x.Name == "sldexp");
            var absexp = existingHashes.First(x => x.Name == "absexp");
            
            long.Parse(sliding.Value.ToString()).Should().Be((long)sld.TotalSeconds);

            long.Parse(absexp.Value.ToString()).Should().Be(abs.ToUnixTimeSeconds());
        }
        
        [Fact]
        public async Task SetProviderKeys()
        {
            // Arrange
            var (sut, cache, _) = fixture.GetSut();
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
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session1 = SharedHelpers.CreateSession();
            var session2 = SharedHelpers.CreateSession(session1.Key);

            var options = new SessionEntryOptions();

            await cache.SetTestSessionAsync(keyCreator.CreateKey<TestSession>(session1.Key),session1);

            // Act
            var result = await sut.AddAsync(session2, options);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionInProgressError>();
            result.Error.As<SessionInProgressError>().Session.ToString().Should().Be(session1.ToString());
        }
    }
}