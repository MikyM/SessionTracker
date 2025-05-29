using System.Globalization;

namespace SessionTracker.Redis.Tests.Integration.RedisDataProvider;

public abstract partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
    public abstract class RefreshAsyncShould<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : RedisFixture
    {
        [Fact]
        public async Task CorrectlyRefreshTheEntry()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();

            var sld = TimeSpan.FromSeconds(15);
            var abs = DateTimeOffset.UtcNow.Add(sld);
            
            await cache.HashSetAsync(keyCreator.CreateKey<TestSession>(session.Key), [ 
                new HashEntry(new RedisValue("data"), new RedisValue(session.ToString())),
                new HashEntry(new RedisValue("absexp"), new RedisValue(DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)).ToUnixTimeSeconds().ToString())),
                new HashEntry(new RedisValue("sldexp"), new RedisValue(sld.TotalSeconds.ToString(CultureInfo.InvariantCulture)))
            ]);

            await cache.KeyExpireAsync(keyCreator.CreateKey<TestSession>(session.Key), sld);
            
            // Act

            await Task.Delay(3);
            
            var expired1 = await cache.KeyExpireTimeAsync(keyCreator.CreateKey<TestSession>(session.Key));

            expired1.Should().NotBeNull();
            expired1.Should().BeCloseTo(abs.ToUniversalTime().UtcDateTime, TimeSpan.FromMilliseconds(100));
            
            var result = await sut.RefreshAsync<TestSession>(session.Key);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            
            var expired = await cache.KeyExpireTimeAsync(keyCreator.CreateKey<TestSession>(session.Key));

            var now = DateTimeOffset.UtcNow;
            expired.Should().NotBeNull();
            expired.Should().BeCloseTo(now.ToUniversalTime().UtcDateTime.Add(sld), TimeSpan.FromMilliseconds(100));
        }
        
        [Fact]
        public async Task ReturnNotFoundErrorWhenNoRegularOrEvictedEntryFound()
        {
            // Arrange
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            
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
            var (sut, cache, keyCreator) = fixture.GetSut();
            var session = SharedHelpers.CreateSession();
            
            await cache.HashSetAsync(keyCreator.CreateEvictedKey<TestSession>(session.Key), [ 
                new HashEntry(new RedisValue("data"), new RedisValue(session.ToString())),
                new HashEntry(new RedisValue("absexp"), new RedisValue(DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)).ToUnixTimeSeconds().ToString())),
                new HashEntry(new RedisValue("sldexp"), new RedisValue("1234"))
            ]);
            
            // Act
            var result = await sut.RefreshAsync<TestSession>(session.Key);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<SessionAlreadyEvictedError>();
        }
    }
}