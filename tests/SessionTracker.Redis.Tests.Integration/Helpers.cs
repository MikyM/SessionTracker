using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SessionTracker.Redis.Tests.Integration;

public static class Helpers
{
    /*
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
    }*/
}