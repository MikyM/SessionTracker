//
//  ServiceCollectionExtensions.cs
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

using Microsoft.Extensions.DependencyInjection;
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

        return new SessionTrackerBuilder(services, config);
    }
}
