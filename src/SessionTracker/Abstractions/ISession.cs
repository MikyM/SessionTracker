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

namespace SessionTracker.Abstractions;

/// <summary>
/// Represents a basic session.
/// </summary>
[PublicAPI]
public interface ISession
{
    /// <summary>
    /// Session's key.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the time at which this session started.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// The version of the session for concurrent operation checks.
    /// </summary>
    long Version { get; }
}
