//
//  ISession.cs
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

using System.Diagnostics.CodeAnalysis;

namespace SessionTracker.Abstractions;

/// <summary>
/// Represents a basic session.
/// </summary>
[PublicAPI]
public interface ISession
{
    /// <summary>
    /// Session's Id.
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// The underlying provider key associated with this session.
    /// </summary>
    string? ProviderKey { get; }
    
    /// <summary>
    /// The underlying evicted provider key associated with this session.
    /// </summary>
    string? EvictedProviderKey { get; }

    /// <summary>
    /// Gets the time at which this session started.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// The version of the session for concurrent operation checks.
    /// </summary>
    long Version { get; }

    /// <summary>
    /// Sets the provider keys.
    /// </summary>
    /// <param name="regularKey">Regular session key in the underlying storage.</param>
    /// <param name="evictedKey">Evicted session key in the underlying storage.</param>
    [MemberNotNull(nameof(ProviderKey), nameof(EvictedProviderKey))]
    void SetProviderKeys(string regularKey, string evictedKey);
}
