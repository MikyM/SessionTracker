//
//  SessionEntryOptions.cs
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


// ReSharper disable NonReadonlyMemberInGetHashCode
namespace SessionTracker;

/// <summary>
/// Provides the cache options for an entry in <see cref="ISessionTracker"/>.
/// </summary>
[PublicAPI]
public class SessionEntryOptions : IEquatable<SessionEntryOptions>
{
    private TimeSpan? _absoluteExpirationRelativeToNow;
    private TimeSpan? _slidingExpiration;

    /// <summary>
    /// Gets or sets an absolute expiration date for the cache entry.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    
    /// <summary>
    /// Sets an absolute expiration time, relative to now.
    /// </summary>
    /// <param name="relative">The expiration time, relative to now.</param>
    public SessionEntryOptions SetAbsoluteExpiration(TimeSpan relative)
    {
        AbsoluteExpirationRelativeToNow = relative;
        return this;
    }

    /// <summary>
    /// Sets an absolute expiration date for the cache entry.
    /// </summary>
    /// <param name="absolute">The expiration time, in absolute terms.</param>
    public SessionEntryOptions SetAbsoluteExpiration(DateTimeOffset absolute)
    {
        AbsoluteExpiration = absolute;
        return this;
    }

    /// <summary>
    /// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
    /// This will not extend the entry lifetime beyond the absolute expiration (if set).
    /// </summary>
    /// <param name="offset">The sliding expiration time.</param>
    public SessionEntryOptions SetSlidingExpiration(TimeSpan offset)
    {
        SlidingExpiration = offset;
        return this;
    }

    /// <summary>
    /// Gets or sets an absolute expiration time, relative to now.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow
    {
        get => _absoluteExpirationRelativeToNow;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(AbsoluteExpirationRelativeToNow),
                    value,
                    "The relative expiration value must be positive.");
            }

            _absoluteExpirationRelativeToNow = value;
        }
    }

    /// <summary>
    /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
    /// This will not extend the entry lifetime beyond the absolute expiration (if set).
    /// </summary>
    public TimeSpan? SlidingExpiration
    {
        get => _slidingExpiration;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(SlidingExpiration),
                    value,
                    "The sliding expiration value must be positive.");
            }

            _slidingExpiration = value;
        }
    }

    /// <inheritdoc />
    public bool Equals(SessionEntryOptions? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Nullable.Equals(_absoluteExpirationRelativeToNow, other._absoluteExpirationRelativeToNow) &&
               Nullable.Equals(_slidingExpiration, other._slidingExpiration) &&
               Nullable.Equals(AbsoluteExpiration, other.AbsoluteExpiration);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SessionEntryOptions)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_absoluteExpirationRelativeToNow, _slidingExpiration, AbsoluteExpiration);
    }

    /// <summary>
    /// Checks whether these instances are equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(SessionEntryOptions? left, SessionEntryOptions? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Checks whether these instances are not equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(SessionEntryOptions? left, SessionEntryOptions? right)
    {
        return !Equals(left, right);
    }
}
