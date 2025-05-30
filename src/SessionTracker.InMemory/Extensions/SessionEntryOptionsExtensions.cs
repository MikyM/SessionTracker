using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace SessionTracker.InMemory.Extensions;

/// <summary>
/// Extensions for <see cref="SessionEntryOptions"/>.
/// </summary>
[PublicAPI]
public static class SessionEntryOptionsExtensions
{
    /// <summary>
    /// Converts an instance of <see cref="SessionEntryOptions"/> to <see cref="MemoryCacheEntryOptions"/>.
    /// </summary>
    /// <param name="options">Options to convert.</param>
    /// <returns>Converted options.</returns>
    public static MemoryCacheEntryOptions ToMemoryCacheEntryOptions(this SessionEntryOptions options)
        => new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = options.AbsoluteExpiration,
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options.SlidingExpiration
        };
}