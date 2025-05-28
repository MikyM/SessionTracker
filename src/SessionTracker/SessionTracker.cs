//
//  SessionTracker.cs
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

using Microsoft.Extensions.Options;
using Remora.Results;
using SessionTracker.Abstractions;

namespace SessionTracker;

/// <inheritdoc/>
[PublicAPI]
public class SessionTracker : ISessionTracker
{
    private readonly ISessionLockProvider _lockProvider;
    private readonly ISessionDataProvider _dataProvider;
    private readonly SessionTrackerSettings _cacheTrackerSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTracker"/> class.
    /// </summary>
    /// <param name="dataProvider">The cache data provider.</param>
    /// <param name="lockProvider">The lock provider.</param>
    /// <param name="settings">The cache settings.</param>
    public SessionTracker(ISessionDataProvider dataProvider, ISessionLockProvider lockProvider, IOptions<SessionTrackerSettings> settings)
    {
        _dataProvider = dataProvider;
        _lockProvider = lockProvider;
        _cacheTrackerSettings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> GetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.GetAsync<TSession>(key, ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> GetFinishedAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            return await _dataProvider.GetEvictedAsync<TSession>(key, ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ILockedSession<TSession>>> GetLockedAsync<TSession>(string key,
        TimeSpan? lockExpirationTime = null, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        ISessionLock? @lock = null;
        try
        {
            var lockRes = await _lockProvider.AcquireAsync<TSession>(key,
                lockExpirationTime ?? _cacheTrackerSettings.GetLockExpirationOrDefault<TSession>(), ct);

            if (!lockRes.IsDefined(out @lock))
                return Result<ILockedSession<TSession>>.FromError(lockRes);

            var getResult = await _dataProvider.GetAsync<TSession>(key, ct);
            if (!getResult.IsDefined(out var session))
                return Result<ILockedSession<TSession>>.FromError(getResult);

            return new LockedSession<TSession>(session, @lock);
        }
        catch (Exception ex)
        {
            if (@lock is not null)
                await @lock.DisposeAsync();
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ILockedSession<TSession>>> GetLockedAsync<TSession>(string key,
        TimeSpan lockExpirationTime, TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        ISessionLock? @lock = null;
        try
        {
            var lockRes = await _lockProvider.AcquireAsync<TSession>(key,
                lockExpirationTime, lockWaitTime, lockRetryTime, ct);

            if (!lockRes.IsDefined(out @lock))
                return Result<ILockedSession<TSession>>.FromError(lockRes);

            var getResult = await _dataProvider.GetAsync<TSession>(key, ct);
            if (!getResult.IsDefined(out var session))
                return Result<ILockedSession<TSession>>.FromError(getResult);

            return new LockedSession<TSession>(session, @lock);
        }
        catch (Exception ex)
        {
            if (@lock is not null)
                await @lock.DisposeAsync();
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ILockedSession<TSession>>> GetLockedAsync<TSession>(string key, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        ISessionLock? @lock = null;
        try
        {
            var lockRes = await _lockProvider.AcquireAsync<TSession>(key,
                _cacheTrackerSettings.GetLockExpirationOrDefault<TSession>(), lockWaitTime, lockRetryTime, ct);

            if (!lockRes.IsDefined(out @lock))
                return Result<ILockedSession<TSession>>.FromError(lockRes);

            var getResult = await _dataProvider.GetAsync<TSession>(key, ct);
            if (!getResult.IsDefined(out var session))
                return Result<ILockedSession<TSession>>.FromError(getResult);

            return new LockedSession<TSession>(session, @lock);
        }
        catch (Exception ex)
        {
            if (@lock is not null)
                await @lock.DisposeAsync();
            return new ExceptionError(ex);
        }
    }


    /// <inheritdoc />
    public async Task<Result> StartAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.AddAsync(session, _cacheTrackerSettings.GetSessionEntryOptions<TSession>(), ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RefreshAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await RefreshAsync<TSession>(session.Key, ct);

    /// <inheritdoc />
    public async Task<Result> RefreshAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            return await _dataProvider.RefreshAsync<TSession>(key, ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        session.Version += 1;
        
        try
        {
            return await _dataProvider.UpdateAsync(session, ct);
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

        session.Version += 1;
        
        try
        {
            return await _dataProvider.UpdateAndGetAsync(session, ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan? lockExpirationTime = null,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            return await _lockProvider
                .AcquireAsync<TSession>(key, lockExpirationTime ?? _cacheTrackerSettings.GetLockExpirationOrDefault<TSession>(),
                    ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan lockExpirationTime,
        TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            return await _lockProvider.AcquireAsync<TSession>(key, lockExpirationTime, lockWaitTime, lockRetryTime, ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(string key, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
  
        try
        {
            return await _lockProvider.AcquireAsync<TSession>(key, _cacheTrackerSettings.GetLockExpirationOrDefault<TSession>(),
                lockWaitTime, lockRetryTime, ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan? lockExpirationTime = null,
        CancellationToken ct = default) where TSession : Session
        => await LockAsync<TSession>(session.Key, lockExpirationTime, ct);

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan lockExpirationTime,
        TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
        => await LockAsync<TSession>(session.Key, lockExpirationTime, lockWaitTime, lockRetryTime, ct)
            ;

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
        => await LockAsync<TSession>(session.Key, lockWaitTime, lockRetryTime, ct);

    /// <inheritdoc />
    public async Task<Result> FinishAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await RefreshAsync<TSession>(session.Key, ct);

    /// <inheritdoc />
    public async Task<Result> FinishAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            return await _dataProvider.EvictAsync<TSession>(key,
                _cacheTrackerSettings.GetEvictionSessionEntryOptions<TSession>(), ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> FinishAndGetAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await FinishAndGetAsync<TSession>(session.Key, ct);

    /// <inheritdoc />
    public async Task<Result<TSession>> FinishAndGetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.EvictAndGetAsync<TSession>(key,
                _cacheTrackerSettings.GetEvictionSessionEntryOptions<TSession>(), ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ResumeAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await ResumeAsync<TSession>(session.Key, ct);

    /// <inheritdoc />
    public async Task<Result> ResumeAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.RestoreAsync<TSession>(key,
                _cacheTrackerSettings.GetSessionEntryOptions<TSession>() ??
                throw new InvalidOperationException(), ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> ResumeAndGetAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await ResumeAndGetAsync<TSession>(session.Key, ct);

    /// <inheritdoc />
    public async Task<Result<TSession>> ResumeAndGetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.RestoreAndGetAsync<TSession>(key,
                _cacheTrackerSettings.GetSessionEntryOptions<TSession>() ??
                throw new InvalidOperationException(), ct);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }
}
