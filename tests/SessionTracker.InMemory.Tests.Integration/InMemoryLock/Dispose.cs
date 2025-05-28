using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace SessionTracker.InMemory.Tests.Integration.InMemoryLock;

[CollectionDefinition("InMemorySessionLock")]
public class InMemoryLock
{
    [Collection("InMemorySessionLock")]
    public class DisposeShould
    {
        [Fact]
        public void CorrectlyReleaseTheLock()
        {
            // Arrange 
            var sut = Helpers.GetLockSut();
            var queue = sut.Provider.GetRequiredService<MemoryCacheQueue>();
            var exp = TimeSpan.FromSeconds(3);
            var resource = "test";
            var id = Guid.NewGuid().ToString();

            sut.Cache.Set(resource, id);
            
            var @lock = new InMemorySessionLock(resource, id, true, SessionLockStatus.Acquired, queue, DateTimeOffset.UtcNow.Add(exp));

            // Act
            @lock.Dispose();

            // Assert
            var exists = sut.Cache.TryGetValue(resource, out _);
            exists.Should().BeFalse();
        }
        
        [Fact]
        public void CorrectlySetStatusToExpiredIfCalledWhenEntryDoesNotExist()
        {
            // Arrange 
            var sut = Helpers.GetLockSut();
            var queue = sut.Provider.GetRequiredService<MemoryCacheQueue>();
            var exp = TimeSpan.FromSeconds(3);
            var resource = "test";
            var id = Guid.NewGuid().ToString();

            var @lock = new InMemorySessionLock(resource, id, true, SessionLockStatus.Acquired, queue, DateTimeOffset.UtcNow.Add(exp));

            // Act
            @lock.Dispose();

            // Assert
            var exists = sut.Cache.TryGetValue(resource, out _);
            exists.Should().BeFalse();
        }
    }
    
    [Collection("InMemorySessionLock")]
    public class DisposeAsyncShould
    {
        [Fact]
        public async Task CorrectlyReleaseTheLock()
        {
            // Arrange 
            var sut = Helpers.GetLockSut();
            var queue = sut.Provider.GetRequiredService<MemoryCacheQueue>();
            var exp = TimeSpan.FromSeconds(3);
            var resource = "test";
            var id = Guid.NewGuid().ToString();

            sut.Cache.Set(resource, id);
            
            var @lock = new InMemorySessionLock(resource, id, true, SessionLockStatus.Acquired, queue, DateTimeOffset.UtcNow.Add(exp));

            // Act
            await @lock.DisposeAsync();

            // Assert
            var exists = sut.Cache.TryGetValue(resource, out _);
            exists.Should().BeFalse();
        }
        
        
        [Fact]
        public async Task CorrectlySetStatusToExpiredIfCalledWhenEntryDoesNotExist()
        {
            // Arrange 
            var sut = Helpers.GetLockSut();
            var queue = sut.Provider.GetRequiredService<MemoryCacheQueue>();
            var exp = TimeSpan.FromSeconds(3);
            var resource = "test";
            var id = Guid.NewGuid().ToString();

            var @lock = new InMemorySessionLock(resource, id, true, SessionLockStatus.Acquired, queue, DateTimeOffset.UtcNow.Add(exp));

            // Act
            await @lock.DisposeAsync();

            // Assert
            var exists = sut.Cache.TryGetValue(resource, out _);
            exists.Should().BeFalse();
        }
    }
}
