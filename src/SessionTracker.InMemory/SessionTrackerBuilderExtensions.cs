//
//  RedisSessionSettingsExtensions.cs
//


using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SessionTracker.InMemory;

// ReSharper disable once InconsistentNaming
/// <summary>
/// DI extensions.
/// </summary>
[PublicAPI]
public static class SessionTrackerBuilderExtensions
{
    /// <summary>
    /// Adds an in-memory based backing store implementation for sessions.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="settingsConfiguration">Session tracker configuration.</param>
    /// <returns>The options.</returns>
    public static InMemorySessionTrackerBuilder AddInMemoryProviders
    (
        this SessionTrackerBuilder builder, Action<InMemorySessionTrackerSettings> settingsConfiguration
    )
    {
        var memoryOpt = new InMemorySessionTrackerSettings();
        
        settingsConfiguration(memoryOpt);

        builder.Services.AddOptions();
        
        builder.Services.Configure(settingsConfiguration);
        
        builder.Services.TryAddSingleton(TimeProvider.System);

        if (memoryOpt.ShouldRegisterMemoryCache)
        {
            if (memoryOpt.MemoryCacheOptions is not null)
            {
                builder.Services.AddMemoryCache(memoryOpt.MemoryCacheOptions);
            }
            else
            {
                builder.Services.AddMemoryCache();
            }
        }

        builder.AddLockProvider<InMemorySessionLockProvider>();
        
        builder.AddDataProvider<InMemorySessionDataProvider>();

        builder.Services.AddSingleton<InMemorySessionTrackerKeyCreator>();

        builder.Services.AddSingleton<MemoryCacheQueue>();
        
        builder.Services.AddLogging();

        return new InMemorySessionTrackerBuilder(builder);
    }
}
