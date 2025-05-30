//
//  ServiceCollectionExtensions.cs
//


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SessionTracker.Abstractions;

namespace SessionTracker;

// ReSharper disable once InconsistentNaming
/// <summary>
/// DI extensions.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds session tracking services to the container.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="sessionConfiguration">An action to configure the session options.</param>
    /// <returns>The services.</returns>
    public static SessionTrackerBuilder AddSessionTracker
    (
        this IServiceCollection services, Action<SessionTrackerSettings>? sessionConfiguration = null
    )
    {
        sessionConfiguration ??= x => x.SetAbsoluteExpiration<Session>(TimeSpan.FromSeconds(30));
        
        services.AddOptions().Configure(sessionConfiguration);
        
        services.AddSingleton<ISessionTracker,SessionTracker>();
        
        var config = new SessionTrackerSettings();
        sessionConfiguration.Invoke(config);
        
        services.TryAddSingleton(TimeProvider.System);

        return new SessionTrackerBuilder(services, config);
    }
}
