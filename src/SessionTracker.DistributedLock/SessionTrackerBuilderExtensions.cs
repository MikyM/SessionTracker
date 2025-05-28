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
