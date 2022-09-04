//
//  RedisSessionsBackingStoreProvider.cs
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using SessionTracker.Abstractions;
using StackExchange.Redis;

#pragma warning disable CS8774

namespace SessionTracker.Redis;

/// <summary>
/// A redis based implementation of <see cref="ISessionTrackerDataProvider"/>.
/// </summary>
[UsedImplicitly]
[PublicAPI]
public sealed class RedisSessionTrackerDataProvider : ISessionTrackerDataProvider, IDisposable
{
    private static readonly Version ServerVersionWithExtendedSetCommand = new(4, 0, 0);
 
    private readonly IDatabase _cache;
    private volatile IConnectionMultiplexer _multiplexer;

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ISessionLockProvider _lockProvider;

    private bool _disposed;

    private readonly RedisSessionSettings _options;
    private readonly string _instance;
    private readonly ILogger<RedisSessionTrackerDataProvider> _logger;

    private readonly SemaphoreSlim _connectionLock = new(1,1);

    /// <summary>
    /// Initializes a new instance of <see cref="RedisSessionTrackerDataProvider"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <param name="multiplexer">Connection multiplexer.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="jsonOptions">Json options.</param>
    /// <param name="lockProvider">Lock factory.</param>
    public RedisSessionTrackerDataProvider(IOptions<RedisSessionSettings> optionsAccessor, IConnectionMultiplexer multiplexer,
        ILogger<RedisSessionTrackerDataProvider> logger, IOptionsMonitor<JsonSerializerOptions> jsonOptions,
        ISessionLockProvider lockProvider)
    {
        _jsonOptions = jsonOptions.Get(RedisSessionSettings.JsonSerializerName);
        _cache = multiplexer.GetDatabase();
        _multiplexer = multiplexer;
        _lockProvider = lockProvider;

        if (optionsAccessor is null)
            throw new ArgumentNullException(nameof(optionsAccessor));

        _options = optionsAccessor.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // This allows partitioning a single backend cache for use with multiple apps/services.
        _instance = _options.InstanceName ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> GetEvictedAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var result = await _cache.ScriptEvaluateAsync(LuaScripts.GetEvictedScript,
                    new RedisKey[] { CreateEvictedKey<TSession>(key) })
                .ConfigureAwait(false);

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the evicted cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            var deserializationResult = TryDeserialize<TSession>(extracted);
            return deserializationResult.IsDefined(out var session)
                ? session
                : Result<TSession>.FromError(deserializationResult);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }
    

    /// <inheritdoc />
    public async Task<Result> AddAsync<TSession>(TSession session, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var creationTime = DateTimeOffset.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            var serializedResult = TrySerialize(session);
            if (!serializedResult.IsDefined(out var serialized))
                return Result.FromError(serializedResult);

            var result = await _cache.ScriptEvaluateAsync(LuaScripts.SetNotExistsScript,
                new RedisKey[] { CreateKey<TSession>(session.Key) },
                new RedisValue[]
                {
                    absoluteExpiration?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent,
                    options.SlidingExpiration?.TotalSeconds ?? LuaScripts.NotPresent,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? LuaScripts.NotPresent,
                    serialized
                }).ConfigureAwait(false);

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            if (extracted != "1")
            {
                var existingDeserializedResult = TryDeserialize<TSession>(extracted);
                if (!existingDeserializedResult.IsDefined(out var existing))
                    return Result.FromError(existingDeserializedResult);

                return new SessionInProgressError(existing);
            }

            return Result.FromSuccess();
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> GetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var result = await GetAndRefreshPrivateAsync<TSession>(key, true, ct: ct).ConfigureAwait(false);

        return result.IsDefined(out var session) ? session : Result<TSession>.FromError(result);
    }

    private async Task<Result<TSession?>> GetAndRefreshPrivateAsync<TSession>(string key, bool getData,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var result = await _cache.ScriptEvaluateAsync(LuaScripts.GetAndRefreshScript,
                    new RedisKey[] { CreateKey<TSession>(key) },
                    new RedisValue[] { getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg })
                .ConfigureAwait(false);

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnValue)
                return Result<TSession?>.FromSuccess(null);

            var deserializationResult = TryDeserialize<TSession>(extracted);
            return deserializationResult.IsDefined(out var session)
                ? session
                : Result<TSession?>.FromError(deserializationResult);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RefreshAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var result = await GetAndRefreshPrivateAsync<TSession>(key, false, ct).ConfigureAwait(false);

            return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> UpdateAndGetAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var result = await UpdateAndRefreshPrivateAsync(session, true, ct).ConfigureAwait(false);
        return result.IsDefined(out var updatedSession) ? updatedSession : Result<TSession>.FromError(result);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var result = await UpdateAndRefreshPrivateAsync(session, false, ct).ConfigureAwait(false);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result);
    }

    private async Task<Result<TSession?>> UpdateAndRefreshPrivateAsync<TSession>(TSession session, bool getData,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var serializedResult = TrySerialize(session);
            if (!serializedResult.IsDefined(out var serialized))
                return Result<TSession?>.FromError(serializedResult);

            var result = await _cache.ScriptEvaluateAsync(LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript,
                    new RedisKey[] { CreateKey<TSession>(session.Key) },
                    new RedisValue[] { serialized, getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg })
                .ConfigureAwait(false);

            if (result.IsNull)
                return new NotFoundError($"The given key \"{session.Key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnValue)
                return Result<TSession?>.FromSuccess(null);

            var deserializationResult = TryDeserialize<TSession>(extracted);
            return deserializationResult.IsDefined(out var deserializedSession)
                ? deserializedSession
                : Result<TSession?>.FromError(deserializationResult);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan lockExpirationTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var lockRes = await _lockProvider.AcquireAsync(CreateKey<TSession>(key), lockExpirationTime, ct)
                .ConfigureAwait(false);
            return !lockRes.IsDefined(out var @lock) ? lockRes : Result<ISessionLock>.FromSuccess(@lock);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan lockExpirationTime,
        TimeSpan waitTime,
        TimeSpan retryTime, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var lockRes = await _lockProvider
                .AcquireAsync(CreateKey<TSession>(key), lockExpirationTime, waitTime, retryTime, ct)
                .ConfigureAwait(false);
            return !lockRes.IsDefined(out var @lock) ? lockRes : Result<ISessionLock>.FromSuccess(@lock);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> EvictAsync<TSession>(string key, TimeSpan evictedExpiration,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await EvictAndGetPrivateAsync<TSession>(key, evictedExpiration, false, ct).ConfigureAwait(false);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result);
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> EvictAndGetAsync<TSession>(string key, TimeSpan evictedExpiration,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await EvictAndGetPrivateAsync<TSession>(key, evictedExpiration, true, ct).ConfigureAwait(false);
        return result.IsDefined(out var session) ? session : Result<TSession>.FromError(result);
    }

    private async Task<Result<TSession?>> EvictAndGetPrivateAsync<TSession>(string key, TimeSpan evictedExpiration,
        bool getData, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var result = await _cache.ScriptEvaluateAsync(LuaScripts.RemoveMoveToEvictedScript,
                new RedisKey[] { CreateKey<TSession>(key) },
                new RedisValue[]
                {
                    GetEvictedExpirationInSeconds(evictedExpiration) ?? LuaScripts.NotPresent,
                    getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg,
                    CreateEvictedKey<TSession>(key)
                }).ConfigureAwait(false);

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnValue)
                return Result<TSession?>.FromSuccess(null);

            if (extracted == LuaScripts.SuccessfulRemoveScriptEvictedExistsReturnValue)
                return new SessionAlreadyFinishedError();

            var deserializationResult = TryDeserialize<TSession>(extracted);
            return deserializationResult.IsDefined(out var session)
                ? session
                : Result<TSession?>.FromError(deserializationResult);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RestoreAsync<TSession>(string key, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await RestoreAndGetPrivateAsync<TSession>(key, options, false, ct).ConfigureAwait(false);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result);
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> RestoreAndGetAsync<TSession>(string key, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await RestoreAndGetPrivateAsync<TSession>(key, options, true, ct).ConfigureAwait(false);
        return result.IsDefined(out var session) ? session : Result<TSession>.FromError(result);
    }

    private async Task<Result<TSession?>> RestoreAndGetPrivateAsync<TSession>(string key, SessionEntryOptions options,
        bool getData,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var creationTime = DateTimeOffset.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            var result = await _cache.ScriptEvaluateAsync(LuaScripts.RestoreMoveToRegularScript,
                new RedisKey[] { CreateEvictedKey<TSession>(key) },
                new RedisValue[]
                {
                    absoluteExpiration?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent,
                    options.SlidingExpiration?.TotalSeconds ?? LuaScripts.NotPresent,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? LuaScripts.NotPresent,
                    getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg,
                    CreateKey<TSession>(key)
                }).ConfigureAwait(false);

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnValue)
                return Result<TSession?>.FromSuccess(null);

            var deserializationResult = TryDeserialize<TSession>(extracted);
            return deserializationResult.IsDefined(out var session)
                ? session
                : Result<TSession?>.FromError(deserializationResult);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    private void PrepareConnection()
    {
        TryRegisterProfiler();
    }

    private void TryRegisterProfiler()
    {
        if (_options.ProfilingSession != null)
            _multiplexer.RegisterProfiler(_options.ProfilingSession);
    }

    /// <summary>
    /// Gets expiration time in seconds.
    /// </summary>
    /// <param name="creationTime">Creation time.</param>
    /// <param name="absoluteExpiration">Absolute expiration.</param>
    /// <param name="options">Options.</param>
    /// <returns>Expiration time in seconds</returns>
    public static long? GetExpirationInSeconds(DateTimeOffset creationTime, DateTimeOffset? absoluteExpiration,
        SessionEntryOptions options)
    {
        if (absoluteExpiration.HasValue && options.SlidingExpiration.HasValue)
        {
            return (long)Math.Min(
                (absoluteExpiration.Value - creationTime).TotalSeconds,
                options.SlidingExpiration.Value.TotalSeconds);
        }

        if (absoluteExpiration.HasValue)
        {
            return (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
        }

        if (options.SlidingExpiration.HasValue)
        {
            return (long)options.SlidingExpiration.Value.TotalSeconds;
        }
        
        return null;
    }
 
    /// <summary>
    /// Gets absolute expiration.
    /// </summary>
    /// <param name="creationTime">Creation time.</param>
    /// <param name="options">Options.</param>
    /// <returns>Absolute expiration.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when absolute expiration time is not in the future.</exception>
    public static DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset creationTime, SessionEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue && options.AbsoluteExpiration <= creationTime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SessionEntryOptions.AbsoluteExpiration),
                options.AbsoluteExpiration.Value,
                "The absolute expiration session must be in the future.");
        }
 
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return creationTime + options.AbsoluteExpirationRelativeToNow;
        }
 
        return options.AbsoluteExpiration;
    }

    /// <summary>
    /// Gets evicted expiration in seconds.
    /// </summary>
    /// <param name="expiration">Expiration time.</param>
    /// <returns>Evicted expiration in seconds.</returns>
    public static long? GetEvictedExpirationInSeconds(TimeSpan expiration)
        => expiration.TotalSeconds <= 0 ? null : (long)expiration.TotalSeconds;
    
    private Result<TSession> TryDeserialize<TSession>(string session) where TSession : Session
    {
        TSession? deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize<TSession>(session, _jsonOptions);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }

        return deserialized;
    }

    private Result<string> TrySerialize<TSession>(TSession session)  where TSession : Session
    {
        string? serialized;
        try
        {
            serialized = JsonSerializer.Serialize(session, _jsonOptions);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }

        return serialized;
    }

    private string CreateKey<TSession>(string initKey) where TSession : Session
        => $"{(string.IsNullOrWhiteSpace(_instance) ? string.Empty : $"{_instance}:")}{_options.KeyPrefix}:{typeof(TSession).Name.ToLower()}:{initKey}";
    
    private string CreateEvictedKey<TSession>(string initKey) where TSession : Session
        => $"{(string.IsNullOrWhiteSpace(_instance) ? string.Empty : $"{_instance}:")}{_options.KeyPrefix}:evicted:{typeof(TSession).Name.ToLower()}:{initKey}";
    
    /// <summary>
    /// Disposes of this instance and the connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
 
        _disposed = true;
        _multiplexer.Close();
    }
 
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
