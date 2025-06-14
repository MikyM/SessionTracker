﻿//
//  RedisSessionsBackingStoreProvider.cs
//


using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Results;
using SessionTracker.Abstractions;
using SessionTracker.Redis.Abstractions;

#pragma warning disable CS8774

namespace SessionTracker.Redis;

/// <summary>
/// A redis based implementation of <see cref="ISessionDataProvider"/>.
/// </summary>
[UsedImplicitly]
[PublicAPI]
public sealed class RedisSessionDataProvider : ISessionDataProvider
{
    private static readonly Version ServerVersionWithExtendedSetCommand = new(4, 0, 0);
    
    private readonly RedisSessionTrackerKeyCreator _keyCreator;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RedisSessionTrackerSettings _options;
    private readonly ILogger<RedisSessionDataProvider> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IRedisConnectionMultiplexerProvider _multiplexerProvider;
    
    private Dictionary<string, (byte[] Sha1, string Raw)> _knownInternalScripts = [];

    private async Task<bool?> IsUsingProxyAsync()
    {
        var options = await _multiplexerProvider.GetConfigurationOptionsAsync();
        
        return options.Proxy != Proxy.None;
    } 

    private async Task<bool> ShouldUseShaOptimizationAsync()
    {
        var isUsingProxy = await IsUsingProxyAsync();

        return isUsingProxy.HasValue && isUsingProxy.Value && _options.UseBandwidthOptimizationForProxies;
    }
    
    private const string NoScript = "NOSCRIPT";

    private void PreHashScripts()
    {
        _knownInternalScripts.TryAdd(nameof(LuaScripts.GetAndRefreshScript), (CalculateScriptHash(LuaScripts.GetAndRefreshScript), LuaScripts.GetAndRefreshScript));
        _knownInternalScripts.TryAdd(nameof(LuaScripts.GetAndRefreshEvictedScript), (CalculateScriptHash(LuaScripts.GetAndRefreshEvictedScript), LuaScripts.GetAndRefreshEvictedScript));
        _knownInternalScripts.TryAdd(nameof(LuaScripts.SetNotExistsAndReturnScript), (CalculateScriptHash(LuaScripts.SetNotExistsAndReturnScript), LuaScripts.SetNotExistsAndReturnScript));
        _knownInternalScripts.TryAdd(nameof(LuaScripts.RemoveMoveToEvictedScript), (CalculateScriptHash(LuaScripts.RemoveMoveToEvictedScript), LuaScripts.RemoveMoveToEvictedScript));
        _knownInternalScripts.TryAdd(nameof(LuaScripts.RestoreMoveToRegularScript), (CalculateScriptHash(LuaScripts.RestoreMoveToRegularScript), LuaScripts.RestoreMoveToRegularScript));
        _knownInternalScripts.TryAdd(nameof(LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript), (CalculateScriptHash(LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript), LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript));
    }
    
    /// <summary>
    /// Calculates the hash of a script.
    /// </summary>
    /// <param name="script">Script.</param>
    /// <returns>Hash.</returns>
    private static byte[] CalculateScriptHash(string script)
    {
        var scriptBytes = Encoding.ASCII.GetBytes(script);
        return SHA1.HashData(scriptBytes);
    }

    private const string ErrorMessage = "Failed to execute Redis SessionTracker script, check exceptions for more details";
    
    /// <summary>
    /// Initializes a new instance of <see cref="RedisSessionDataProvider"/>.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <param name="multiplexerProvider">Connection multiplexer provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="jsonOptions">Json options.</param>
    /// <param name="keyCreator">The key creator.</param>
    /// <param name="timeProvider">Time provider.</param>
    public RedisSessionDataProvider(IOptions<RedisSessionTrackerSettings> optionsAccessor, IRedisConnectionMultiplexerProvider multiplexerProvider,
        ILogger<RedisSessionDataProvider> logger, IOptionsMonitor<JsonSerializerOptions> jsonOptions, RedisSessionTrackerKeyCreator keyCreator, TimeProvider timeProvider)
    {
        _jsonOptions = jsonOptions.Get(RedisSessionTrackerSettings.JsonSerializerName);
        _multiplexerProvider = multiplexerProvider;

        ArgumentNullException.ThrowIfNull(optionsAccessor);

        _options = optionsAccessor.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyCreator = keyCreator;
        _timeProvider = timeProvider;
        
        PreHashScripts();
    }
    
    private async Task<Result<RedisResult>> ProcessAsync(string scriptName, RedisKey[] keys, RedisValue[] values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var scriptData = _knownInternalScripts[scriptName];

            Result<RedisResult> result;

            if (await ShouldUseShaOptimizationAsync())
            {
                result = await EvalScriptOptimizedAsync(keys, values, scriptData.Sha1, scriptData.Raw,
                    cancellationToken);
            }
            else
            {
                result = await EvalScriptAsync(keys, values, scriptData.Sha1, scriptData.Raw, cancellationToken);
            }

            return result;
        }
        catch (RedisServerException redisServerException)
        {
            return new RedisServerError(redisServerException);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorMessage);
            
            return ex;
        }
    }

    private async Task<IDatabase> GetDatabaseAsync()
        => (await _multiplexerProvider.GetConnectionMultiplexerAsync()).GetDatabase(_options.Database);
    
    private async Task<Result<RedisResult>> EvalScriptOptimizedAsync(RedisKey[] keys, RedisValue[] values, byte[] sha1, 
        string script, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        RedisResult? result = null;
        
        try
        {
            result = await (await GetDatabaseAsync()).ScriptEvaluateAsync(sha1, keys,
                values);
        }
        catch (RedisServerException redisServerException) when (redisServerException.Message.Contains(NoScript))
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < _options.BandwidthOptimizationNoScriptRetriesLimit; i++)
            {
                try
                {
                    result = await (await GetDatabaseAsync()).ScriptEvaluateAsync(script, keys,
                        values, CommandFlags.NoScriptCache);
                    
                    break;
                }
                catch (RedisServerException retriedServerException) when (retriedServerException.Message.Contains(NoScript))
                {
                    // ignore
                }
            }
        }

        return result is null 
            ? new RedisBandwidthScriptOptimizationError()
            : result;
    }

    private async Task<Result<RedisResult>> EvalScriptAsync(RedisKey[] keys, RedisValue[] values,
        byte[] sha1, string script, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await (await GetDatabaseAsync()).ScriptEvaluateAsync(script, keys, values);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> GetEvictedAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var keys = _keyCreator.CreateKeys<TSession>(key);

        try
        {
            var subResult = await ProcessAsync(nameof(LuaScripts.GetAndRefreshEvictedScript),
                [ keys.Evicted ],
                [
                    "1",
                    keys.Regular
                ], ct);

            if (!subResult.IsDefined(out var result))
            {
                return Result<TSession>.FromError(subResult);
            }

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the evicted cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            if (extracted == LuaScripts.UnsuccessfulScriptOtherCacheHasKeyReturnedValue)
                return new SessionAlreadyRestoredError();

            var deserializationResult = TryDeserialize<TSession>(extracted);
            return deserializationResult.IsDefined(out var session)
                ? session
                : Result<TSession>.FromError(deserializationResult);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddAsync<TSession>(TSession session, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        var keys = _keyCreator.CreateKeys<TSession>(session.Key);
        
        session.SetProviderKeys(keys.Regular, keys.Evicted);

        try
        {
            var creationTime = _timeProvider.GetUtcNow();

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            var serializedResult = TrySerialize(session);
            if (!serializedResult.IsDefined(out var serialized))
                return Result.FromError(serializedResult);

            var subResult = await ProcessAsync(nameof(LuaScripts.SetNotExistsAndReturnScript),
                [keys.Regular],
                [
                    absoluteExpiration?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent,
                    options.SlidingExpiration?.TotalSeconds ?? LuaScripts.NotPresent,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? LuaScripts.NotPresent,
                    serialized,
                   keys.Evicted
                ], ct);
            
            if (!subResult.IsDefined(out var result))
            {
                return Result.FromError(subResult);
            }

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

        var result = await GetAndRefreshPrivateAsync<TSession>(key, true, ct: ct);

        return result.IsDefined(out var session) ? session : Result<TSession>.FromError(result);
    }

    private async Task<Result<TSession?>> GetAndRefreshPrivateAsync<TSession>(string key, bool getData,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var keys = _keyCreator.CreateKeys<TSession>(key);
        
        try
        {
            var subResult = await ProcessAsync(nameof(LuaScripts.GetAndRefreshScript),
                    [keys.Regular],
                    [
                        getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg,
                       keys.Evicted
                    ], ct);
            
            if (!subResult.IsDefined(out var result))
            {
                return Result<TSession?>.FromError(subResult);
            }

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);

            if (extracted == LuaScripts.UnsuccessfulScriptOtherCacheHasKeyReturnedValue)
                return new SessionAlreadyEvictedError();

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnedValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnedValue)
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
            var result = await GetAndRefreshPrivateAsync<TSession>(key, false, ct);

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

        var result = await UpdateAndRefreshPrivateAsync(session, true, ct);
        return result.IsDefined(out var updatedSession) ? updatedSession : Result<TSession>.FromError(result);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var result = await UpdateAndRefreshPrivateAsync(session,false, ct);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result);
    }

    private async Task<Result<TSession?>> UpdateAndRefreshPrivateAsync<TSession>(TSession session, bool getData,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        var keys = _keyCreator.CreateKeys<TSession>(session.Key);

        try
        {
            var serializedResult = TrySerialize(session);
            if (!serializedResult.IsDefined(out var serialized))
                return Result<TSession?>.FromError(serializedResult);

            var subResult = await ProcessAsync(nameof(LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript),
                    [keys.Regular],
                    [serialized, getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg,keys.Evicted
                    ], ct);
            
            if (!subResult.IsDefined(out var result))
            {
                return Result<TSession?>.FromError(subResult);
            }

            if (result.IsNull)
                return new NotFoundError($"The given key \"{session.Key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);
            
            if (extracted == LuaScripts.UnsuccessfulScriptOtherCacheHasKeyReturnedValue)
                return new SessionAlreadyEvictedError();

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnedValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnedValue)
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
    public async Task<Result> EvictAsync<TSession>(string key, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await EvictAndGetPrivateAsync<TSession>(key, options, false, ct);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result);
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> EvictAndGetAsync<TSession>(string key, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await EvictAndGetPrivateAsync<TSession>(key, options, true, ct);
        return result.IsDefined(out var session) ? session : Result<TSession>.FromError(result);
    }

    private async Task<Result<TSession?>> EvictAndGetPrivateAsync<TSession>(string key, SessionEntryOptions options,
        bool getData, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        var keys = _keyCreator.CreateKeys<TSession>(key);
        
        try
        {
            var creationTime = _timeProvider.GetUtcNow();

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);
            
            var subResult = await ProcessAsync(nameof(LuaScripts.RemoveMoveToEvictedScript),
                [keys.Regular],
                [
                    absoluteExpiration?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent,
                    options.SlidingExpiration?.TotalSeconds ?? LuaScripts.NotPresent,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? LuaScripts.NotPresent,
                    getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg,
                   keys.Evicted
                ], ct);
                
            if (!subResult.IsDefined(out var result))
            {
                return Result<TSession?>.FromError(subResult);
            }

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);
            
            if (extracted == LuaScripts.UnsuccessfulScriptOtherCacheHasKeyReturnedValue)
                return new SessionAlreadyEvictedError();

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnedValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnedValue)
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
    public async Task<Result> RestoreAsync<TSession>(string key, SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await RestoreAndGetPrivateAsync<TSession>(key, options, false, ct);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result);
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> RestoreAndGetAsync<TSession>(string key,
        SessionEntryOptions options,
        CancellationToken ct = default) where TSession : Session
    {
        var result = await RestoreAndGetPrivateAsync<TSession>(key, options, true, ct);
        return result.IsDefined(out var session) ? session : Result<TSession>.FromError(result);
    }

    private async Task<Result<TSession?>> RestoreAndGetPrivateAsync<TSession>(string key, SessionEntryOptions options,
        bool getData,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        var keys = _keyCreator.CreateKeys<TSession>(key);

        try
        {
            var creationTime = _timeProvider.GetUtcNow();

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            var subResult = await ProcessAsync(nameof(LuaScripts.RestoreMoveToRegularScript),
                [keys.Evicted],
                [
                    absoluteExpiration?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent,
                    options.SlidingExpiration?.TotalSeconds ?? LuaScripts.NotPresent,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? LuaScripts.NotPresent,
                    getData ? LuaScripts.ReturnDataArg : LuaScripts.DontReturnDataArg,
                    keys.Regular
                ], ct);
            
            if (!subResult.IsDefined(out var result))
            {
                return Result<TSession?>.FromError(subResult);
            }

            if (result.IsNull)
                return new NotFoundError($"The given key \"{key}\" held no session in the cache.");

            if (!result.TryExtractString(out var extracted))
                return new UnexpectedRedisResultError(result);
            
            if (extracted == LuaScripts.UnsuccessfulScriptOtherCacheHasKeyReturnedValue)
                return new SessionAlreadyRestoredError();

            if (!getData && extracted != LuaScripts.SuccessfulScriptNoDataReturnedValue)
                return new UnexpectedRedisResultError(result);

            if (!getData && extracted == LuaScripts.SuccessfulScriptNoDataReturnedValue)
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
}
