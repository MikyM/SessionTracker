//
//  Session.cs
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
using System.Text.Json.Serialization;
using SessionTracker.Abstractions;

namespace SessionTracker;

/// <summary>
/// Represents a basic session.
/// </summary>
[PublicAPI]
public class Session : ISession, IEquatable<ISession>
{
    /// <summary>
    /// Base session constructor.
    /// </summary>
    /// <param name="key">Sessions key.</param>
    public Session(string key)
    {
        Key = key;
    }

    /// <summary>
    /// The version of the session.
    /// </summary>
    [JsonInclude]
    public long Version { get; internal set; } = 1;

    /// <summary>
    /// Gets the time at which this session started.
    /// </summary>
    [JsonInclude]
    public DateTimeOffset StartedAt { get; internal set; } = DateTimeProvider.Instance.OffsetUtcNow;

    /// <summary>
    /// The key associated with this session.
    /// </summary>
    [JsonInclude]
    public string Key { get; internal set; }

    /// <summary>
    /// Serializes this instance to JSON.
    /// </summary>
    /// <returns>JSON representation of current instance.</returns>
    public override string ToString()
        => JsonSerializer.Serialize(this);

    /// <inheritdoc />
    public bool Equals(ISession? other)
    {
        if (other is null)
            return false;

        return Key == other.Key;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Session)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        => Key.GetHashCode();
    
    /// <summary>
    /// Compares two locked sessions.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Session? left, Session? right)
        => Equals(left, right);
    
    /// <summary>
    /// Compares two locked sessions.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Session? left, Session? right)
        => !Equals(left, right);
}
