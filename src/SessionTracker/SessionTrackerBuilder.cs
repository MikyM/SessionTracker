using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SessionTracker.Abstractions;

namespace SessionTracker;

/// <summary>
/// Session tracker builder.
/// </summary>
[PublicAPI]
public class SessionTrackerBuilder
{
    /// <summary>
    /// Services.
    /// </summary>
    public IServiceCollection Services { get; }
    
    /// <summary>
    /// Services.
    /// </summary>
    public SessionTrackerSettings Settings { get; }
    
    internal SessionTrackerBuilder(IServiceCollection services, SessionTrackerSettings settings)
    {
        Services = services;
        Settings = settings;
    }

    /// <summary>
    /// Finalizes the configuration.
    /// </summary>
    /// <remarks>
    /// You don't have to call this method. This is just a way to chain calls to <see cref="IServiceCollection"/>.
    /// </remarks>
    /// <returns>The service collection.</returns>
    public IServiceCollection Complete()
        => Services;

    /// <summary>
    /// Adds a custom session tracking data provider.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <returns>The services.</returns>
    public SessionTrackerBuilder AddDataProvider<TDataProvider>(ServiceLifetime lifetime = ServiceLifetime.Singleton) where TDataProvider : class, ISessionDataProvider
    {
        Services.TryAdd(ServiceDescriptor.Describe(typeof(ISessionDataProvider), typeof(TDataProvider), lifetime));

        return this;
    }
    
    /// <summary>
    /// Adds a custom session tracking lock provider.
    /// </summary>
    /// <returns>The services.</returns>
    public SessionTrackerBuilder AddLockProvider<TLockProvider>(ServiceLifetime lifetime = ServiceLifetime.Singleton) where TLockProvider : class, ISessionLockProvider
    {
        Services.TryAdd(ServiceDescriptor.Describe(typeof(ISessionLockProvider), typeof(TLockProvider), lifetime));

        return this;
    }

    /// <summary>
    /// Adds a custom session tracking data provider.
    /// </summary>
    /// <param name="instance">An instance of the provider.</param>
    /// <returns>The services.</returns>
    public SessionTrackerBuilder AddDataProvider<TDataProvider>(TDataProvider instance) where TDataProvider : class, ISessionDataProvider
    {
        Services.TryAddSingleton<ISessionDataProvider>(instance);

        return this;
    }
    
    /// <summary>
    /// Adds a custom session tracking data provider.
    /// </summary>
    /// <param name="factory">An instance of the provider.</param>
    /// <param name="lifetime">Lifetime.</param>
    /// <returns>The services.</returns>
    public SessionTrackerBuilder AddDataProvider<TDataProvider>(Func<IServiceProvider,TDataProvider> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton) where TDataProvider : class, ISessionDataProvider
    {
        Services.TryAdd(ServiceDescriptor.Describe(typeof(ISessionDataProvider), factory, lifetime));
        return this;
    }

    /// <summary>
    /// Adds a custom session tracking lock provider.
    /// </summary>
    /// <param name="factory">An instance of the provider.</param>
    /// <param name="lifetime">Lifetime.</param>
    /// <returns>The services.</returns>
    public SessionTrackerBuilder AddLockProvider<TLockProvider>(Func<IServiceProvider,TLockProvider> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton) where TLockProvider : class, ISessionLockProvider
    {
        Services.TryAdd(ServiceDescriptor.Describe(typeof(ISessionLockProvider), factory, lifetime));;

        return this;
    }
    
    /// <summary>
    /// Adds a custom session tracking lock provider.
    /// </summary>
    /// <param name="instance">An instance of the provider.</param>
    /// <returns>The services.</returns>
    public SessionTrackerBuilder AddLockProvider<TLockProvider>(TLockProvider instance) where TLockProvider : class, ISessionLockProvider
    {
        Services.AddSingleton<ISessionLockProvider>(instance);

        return this;
    }
}