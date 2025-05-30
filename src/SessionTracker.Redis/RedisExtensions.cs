//
//  RedisExtensions.cs
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

namespace SessionTracker.Redis;

/// <summary>
/// Redis extensions.
/// </summary>
internal static class RedisExtensions
{
    /// <summary>
    /// Attempts to extract a string from a <see cref="RedisResult"/>.
    /// </summary>
    /// <param name="redisResult">A redis result.</param>
    /// <param name="extracted">Extracted string</param>
    /// <returns>True if extraction was successful, false if not.</returns>
    internal static bool TryExtractString(this RedisResult redisResult, [NotNullWhen(true)] out string? extracted)
    {
        extracted = null;
        
        if (redisResult.IsNull)
            return false;
        if (redisResult.Resp3Type is not (ResultType.SimpleString or ResultType.BulkString))
            return false;

        extracted = (string)redisResult!;

        return true;
    }
}
