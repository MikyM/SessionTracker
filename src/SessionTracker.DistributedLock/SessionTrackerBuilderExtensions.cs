//
//  RedisSessionSettingsExtensions.cs
//


using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SessionTracker.DistributedLock;

// ReSharper disable once InconsistentNaming
/// <summary>
/// DI extensions.
/// </summary>
[PublicAPI]
public static class SessionTrackerBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="IDistributedLockProvider"/> based translation layer and lock provider implementation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="settingsConfiguration">Session tracker configuration.</param>
    /// <returns>The options.</returns>
    public static DistributedLockSessionTrackerBuilder AddDistributedLock
    (
        this SessionTrackerBuilder builder, Action<DistributedLockSessionTrackerSettings> settingsConfiguration
    )
    {
        var disOpt = new DistributedLockSessionTrackerSettings();
        
        settingsConfiguration(disOpt);

        builder.Services.AddOptions();
        
        builder.Services.Configure(settingsConfiguration);
        
        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.AddLockProvider<DistributedLockSessionLockProvider>();

        builder.Services.AddSingleton<DistributedLockNameCreator>();

        builder.Services.AddLogging();

        return new DistributedLockSessionTrackerBuilder(builder);
    }
}
