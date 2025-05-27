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

        return new InMemorySessionTrackerBuilder(builder);
    }
}
