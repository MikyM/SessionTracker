//
//  RedisSessionSettingsExtensions.cs
//
//  Author:
//       Krzysztof Kupisz <kupisz.krzysztof@gmail.com>
//
//  Copyright (c) Krzysztof Kupisz
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using SessionTracker.Abstractions;
using StackExchange.Redis;

namespace SessionTracker.Redis;

// ReSharper disable once InconsistentNaming
/// <summary>
/// DI extensions.
/// </summary>
[PublicAPI]
public static class RedisSessionSettingsExtensions
{
    /// <summary>
    /// Adds a redis based backing store implementation for sessions.
    /// </summary>
    /// <remarks>
    /// It is very important to know that the stock implementation of <see cref="ISessionTrackerDataProvider"/> that this method
    /// adds uses JSON to store values. If JSON is not a desirable format, the caching methods in <see cref="RedisSessionTrackerDataProvider"/> can be provided, or a custom
    /// implementation of <see cref="ISessionTrackerDataProvider"/> can be added to the container.
    /// </remarks>
    /// <param name="services">The services.</param>
    /// <param name="sessionConfiguration">Session tracker configuration.</param>
    /// <param name="redisSessionConfiguration">Redis session tracker configuration.</param>
    /// <returns>The options.</returns>
    public static IServiceCollection AddRedisSessionTracker
    (
        this IServiceCollection services, Action<RedisSessionSettings> redisSessionConfiguration, Action<SessionSettings>? sessionConfiguration = null
    )
    {
        services.AddSessionTracker(sessionConfiguration);
        
        var redisOpt = new RedisSessionSettings();
        redisSessionConfiguration(redisOpt);

        var multiplexer =
            redisOpt.Multiplexer ?? redisOpt.MultiplexerFactory?.Invoke();

        if (multiplexer is null && redisOpt.RedisConfigurationOptions is not null)
        {
            multiplexer = ConnectionMultiplexer.Connect(redisOpt.RedisConfigurationOptions);
        }

        if (multiplexer is null)
            throw new InvalidOperationException();
        
        services.Configure(redisSessionConfiguration);
        
        var factory = RedLockFactory.Create(new List<RedLockMultiplexer> { (ConnectionMultiplexer)multiplexer });
        
        services.AddSessionTrackerLockProvider(new RedisSessionLockProvider(factory));
        services.TryAddSingleton(multiplexer);
        services.AddSessionTrackerDataProvider<RedisSessionTrackerDataProvider>();

        return services;
    }
}
