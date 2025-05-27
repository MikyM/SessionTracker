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

using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RedLockNet;
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
public static class SessionTrackerBuilderExtensions
{
    /// <summary>
    /// Adds a redis based backing store implementation for sessions.
    /// </summary>
    /// <remarks>
    /// It is very important to know that the stock implementation of <see cref="ISessionDataProvider"/> that this method
    /// adds uses JSON to store values. If JSON is not a desirable format, the caching methods in <see cref="RedisSessionDataProvider"/> can be provided, or a custom
    /// implementation of <see cref="ISessionDataProvider"/> can be added to the container.
    /// </remarks>
    /// <param name="builder">The builder.</param>
    /// <param name="redisSessionConfiguration">Redis session tracker configuration.</param>
    /// <returns>The options.</returns>
    public static RedisSessionTrackerBuilder AddRedisProviders
    (
        this SessionTrackerBuilder builder, Action<RedisSessionTrackerSettings> redisSessionConfiguration
    )
    {
        var redisOpt = new RedisSessionTrackerSettings();
        redisSessionConfiguration(redisOpt);

        var multiplexer = redisOpt.Multiplexer ?? redisOpt.MultiplexerFactory?.Invoke();

        if (multiplexer is null && redisOpt.RedisConfigurationOptions is not null)
        {
            multiplexer = ConnectionMultiplexer.Connect(redisOpt.RedisConfigurationOptions);

            if (redisOpt.ProfilingSession is not null)
            {
                multiplexer.RegisterProfiler(redisOpt.ProfilingSession);
            }
        }

        if (multiplexer is null)
        {
            throw new InvalidOperationException();
        }

        builder.Services.AddOptions();
        
        if (redisOpt.JsonSerializerConfiguration is not null)
        {
            builder.Services.Configure(RedisSessionTrackerSettings.JsonSerializerName, redisOpt.JsonSerializerConfiguration);
        }
        
        builder.Services.Configure(redisSessionConfiguration);

        if (redisOpt.SkipLockFactoryCreation)
        {
            var redLockMultiplexer = (RedLockMultiplexer)(ConnectionMultiplexer)multiplexer;
        
            redLockMultiplexer.RedisKeyFormat = redisOpt.SessionKeyPrefix + ":" + redisOpt.SessionLockPrefix + ":{0}";
        
            var factory = RedLockFactory.Create(new List<RedLockMultiplexer> { (ConnectionMultiplexer)multiplexer });

            builder.Services.AddSingleton<IDistributedLockFactory>(factory);
        }
        
        builder.AddLockProvider<RedisSessionLockProvider>();
        
        builder.Services.TryAddSingleton(multiplexer);
        
        builder.AddDataProvider<RedisSessionDataProvider>();

        builder.Services.AddSingleton<RedisSessionTrackerKeyCreator>();

        return new RedisSessionTrackerBuilder(builder);
    }
}
