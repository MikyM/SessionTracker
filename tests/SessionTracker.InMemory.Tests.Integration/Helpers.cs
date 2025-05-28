using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SessionTracker.InMemory.Tests.Integration;

public static class Helpers
{
    public static (InMemorySessionDataProvider Sut, IServiceProvider Provider, IMemoryCache Cache, InMemorySessionTrackerKeyCreator KeyCreator) GetDataSut()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();

        var opt = Options.Create(new InMemorySessionTrackerSettings());

        var keyCreator = new InMemorySessionTrackerKeyCreator(opt);

        services.AddLogging();

        services.AddSingleton(keyCreator);

        services.AddSingleton<MemoryCacheQueue>();

        services.AddSingleton<InMemorySessionDataProvider>();
        
        var provider = services.BuildServiceProvider();
        
        return (provider.GetRequiredService<InMemorySessionDataProvider>(), provider, provider.GetRequiredService<IMemoryCache>(), provider.GetRequiredService<InMemorySessionTrackerKeyCreator>());
    }
    
    public static (InMemorySessionLockProvider Sut, IServiceProvider Provider, IMemoryCache Cache, InMemorySessionTrackerKeyCreator KeyCreator) GetLockSut()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();

        var opt = Options.Create(new InMemorySessionTrackerSettings());

        var keyCreator = new InMemorySessionTrackerKeyCreator(opt);

        services.AddSingleton(keyCreator);
        
        services.AddLogging();

        services.AddSingleton<MemoryCacheQueue>();

        services.AddSingleton<InMemorySessionLockProvider>();
        
        var provider = services.BuildServiceProvider();
        
        return (provider.GetRequiredService<InMemorySessionLockProvider>(), provider, provider.GetRequiredService<IMemoryCache>(), provider.GetRequiredService<InMemorySessionTrackerKeyCreator>());
    }
    
    private static readonly PropertyInfo EntriesField = typeof(MemoryCache).GetProperty("EntriesCollection",
        BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static IReadOnlyDictionary<object, object> GetCacheEntries(IMemoryCache cache)
    {
        var collection = (ICollection)EntriesField.GetValue(cache)!;

        var items = new Dictionary<object, object>();

        foreach (var item in collection)
        {
            var methodInfo = item.GetType().GetProperty("Value");
            var val = (ICacheEntry)methodInfo!.GetValue(item)!;
            items.Add(val.Key, val.Value);
        }

        return items;
    }
    
    public static IReadOnlyList<ICacheEntry> GetRawCacheEntries(IMemoryCache cache)
    {
        var collection = (ICollection)EntriesField.GetValue(cache)!;

        var items = new List<ICacheEntry>();

        foreach (var item in collection)
        {
            var methodInfo = item.GetType().GetProperty("Value");
            var val = (ICacheEntry)methodInfo!.GetValue(item)!;
            items.Add(val);
        }

        return items.AsReadOnly();
    }
}