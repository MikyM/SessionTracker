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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using SessionTracker.Abstractions;
using SessionTracker.Redis.Abstractions;

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

        builder.Services.AddOptions();
        
        if (redisOpt.JsonSerializerConfiguration is not null)
        {
            builder.Services.Configure(RedisSessionTrackerSettings.JsonSerializerName, redisOpt.JsonSerializerConfiguration);
        }
        
        builder.Services.Configure(redisSessionConfiguration);
        
        builder.Services.TryAddSingleton(TimeProvider.System);
        
        builder.Services.TryAddSingleton<IRedisConnectionMultiplexerProvider, RedisConnectionMultiplexerProvider>();
        
        builder.Services.TryAddSingleton<IDistributedLockFactoryProvider, DistributedLockFactoryProvider>();
        
        builder.AddLockProvider<RedisSessionLockProvider>();
        
        builder.AddDataProvider<RedisSessionDataProvider>();

        builder.Services.TryAddSingleton<RedisSessionTrackerKeyCreator>();
        
        builder.Services.AddLogging();

        return new RedisSessionTrackerBuilder(builder);
    }
}
