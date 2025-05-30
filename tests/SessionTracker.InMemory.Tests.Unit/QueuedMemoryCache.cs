using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace SessionTracker.InMemory.Tests.Unit;

[UsedImplicitly]
public class Fixture
{
    private readonly PropertyInfo _entriesField = typeof(MemoryCache).GetProperty("EntriesCollection",
        BindingFlags.NonPublic | BindingFlags.Instance)!;

    public IReadOnlyDictionary<object, object> GetCacheEntries(IMemoryCache cache)
    {
        var collection = (ICollection)_entriesField.GetValue(cache)!;

        var items = new Dictionary<object,object>();

        foreach (var item in collection)
        {
            var methodInfo = item.GetType().GetProperty("Value");
            var val = (ICacheEntry)methodInfo!.GetValue(item)!;
            items.Add(val.Key, val.Value);
        }
        
        return items;
    }
}

[CollectionDefinition("QueuedMemoryCache")]
public class QueuedMemoryCache : ICollectionFixture<Fixture>
{
    [Collection("QueuedMemoryCache")]
    public class ProcessingShould(Fixture fixture)
    {
        [Theory]
        [InlineData(5)]
        [InlineData(20)]
        [InlineData(50)]
        public async Task BeInCorrectFifoOrderForOnlyAsyncCalls(int enqueuedActions)
        {
            // Arrange
            var services = new ServiceCollection().AddMemoryCache();
            var provider = services.BuildServiceProvider();
            var memCache = provider.GetRequiredService<IMemoryCache>();
        
            var keys = Enumerable.Range(1, enqueuedActions).ToArray();
            
            var sut = new MemoryCacheQueue(memCache);

            var testData = keys.Select(x =>
                {
                    Func<IMemoryCache, Task> func = async y =>
                    {
                        await Task.Delay(Random.Shared.Next(0, 500));
                        y.Set(x, new object());
                    };
                    
                    return func;
                }).ToArray();

            // Act
            var tasks = testData.Select(dt => sut.EnqueueAsync(dt)).ToArray();
            
            await Task.WhenAll(tasks);
            
            // Assert
            var entries = fixture.GetCacheEntries(memCache).ToDictionary(x => x.Key, x => x.Value);
            entries.Keys.Should().BeInAscendingOrder();
            entries.Keys.Should().HaveCount(enqueuedActions);
            
            // Cleanup
            await provider.DisposeAsync();
        }
        
        [Theory]
        [InlineData(5)]
        [InlineData(20)]
        [InlineData(50)]
        public async Task BeInCorrectFifoOrderForOnlySyncCalls(int enqueuedActions)
        {
            // Arrange
            var services = new ServiceCollection().AddMemoryCache();
            var provider = services.BuildServiceProvider();
            var memCache = provider.GetRequiredService<IMemoryCache>();
        
            var keys = Enumerable.Range(1, enqueuedActions).ToArray();
            
            var sut = new MemoryCacheQueue(memCache);

            var testData = keys.Select(x =>
            {
                Action<IMemoryCache> func = y =>
                {
                    Task.Delay(Random.Shared.Next(0, 500)).Wait();
                    y.Set(x, new object());
                };
                    
                return func;
            }).ToArray();

            // Act
            var tasks = testData.Select(dt => sut.EnqueueAsync(dt)).ToArray();
            
            await Task.WhenAll(tasks);
            
            // Assert
            var entries = fixture.GetCacheEntries(memCache).ToDictionary(x => x.Key, x => x.Value);
            entries.Keys.Should().BeInAscendingOrder();
            entries.Keys.Should().HaveCount(enqueuedActions);
            
            // Cleanup
            await provider.DisposeAsync();
        }
        
        [Theory]
        [InlineData(5)]
        [InlineData(20)]
        [InlineData(50)]
        public async Task BeInCorrectFifoOrderForMixedCalls(int enqueuedActions)
        {
            // Arrange
            var services = new ServiceCollection().AddMemoryCache();
            var provider = services.BuildServiceProvider();
            var memCache = provider.GetRequiredService<IMemoryCache>();
        
            var keys = Enumerable.Range(1, enqueuedActions).ToArray();
            
            var sut = new MemoryCacheQueue(memCache);

            var testData = new List<object>();
            testData.AddRange(keys.Select<int,object>(x =>
            {
                if (x % 2 == 0)
                {
                    Action<IMemoryCache> sync = y =>
                    {
                        Task.Delay(Random.Shared.Next(0, 500)).Wait();
                        y.Set(x, new object());
                    };

                    return sync;
                }
                
                Func<IMemoryCache, Task> func = async y =>
                {
                    await Task.Delay(Random.Shared.Next(0, 500));
                    y.Set(x, new object());
                };
                    
                return func;
            }));

            // Act
            var tasks = testData.Select(dt =>
            {
                if (dt is Func<IMemoryCache, Task> funcAsync)
                {
                    return sut.EnqueueAsync(funcAsync);
                }

                if (dt is Action<IMemoryCache> sync)
                {
                    return sut.EnqueueAsync(sync);
                }

                return Task.CompletedTask;
            }).ToArray();
            
            await Task.WhenAll(tasks);
            
            // Assert
            var entries = fixture.GetCacheEntries(memCache).ToDictionary(x => x.Key, x => x.Value);
            entries.Keys.Should().BeInAscendingOrder();
            entries.Keys.Should().HaveCount(enqueuedActions);
            
            // Cleanup
            await provider.DisposeAsync();
        }
    }
}