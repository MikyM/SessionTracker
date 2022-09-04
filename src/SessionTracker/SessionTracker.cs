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
    private readonly ISessionTrackerDataProvider _dataProvider;
    private readonly SessionSettings _cacheSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTracker"/> class.
    /// </summary>
    /// <param name="dataProvider">The cache provider.</param>
    /// <param name="cacheSettings">The cache settings.</param>
    public SessionTracker(ISessionTrackerDataProvider dataProvider, IOptions<SessionSettings> cacheSettings)
    {
        _dataProvider = dataProvider;
        _cacheSettings = cacheSettings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> GetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.GetAsync<TSession>(key, ct).ConfigureAwait(false);
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
            return await _dataProvider.GetEvictedAsync<TSession>(key, ct).ConfigureAwait(false);
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
            var lockResult = await _dataProvider.LockAsync<TSession>(key,
                    lockExpirationTime ?? _cacheSettings.GetLockExpirationOrDefault<TSession>(), ct)
                .ConfigureAwait(false);
            if (!lockResult.IsDefined(out @lock))
                return Result<ILockedSession<TSession>>.FromError(lockResult);

            var getResult = await _dataProvider.GetAsync<TSession>(key, ct).ConfigureAwait(false);
            if (!getResult.IsDefined(out var session))
                return Result<ILockedSession<TSession>>.FromError(getResult);

            return new LockedSession<TSession>(session, @lock);
        }
        catch (Exception ex)
        {
            if (@lock is not null)
                await @lock.DisposeAsync().ConfigureAwait(false);
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
            var lockResult = await _dataProvider.LockAsync<TSession>(key,
                    lockExpirationTime, lockWaitTime, lockRetryTime, ct)
                .ConfigureAwait(false);
            if (!lockResult.IsDefined(out @lock))
                return Result<ILockedSession<TSession>>.FromError(lockResult);

            var getResult = await _dataProvider.GetAsync<TSession>(key, ct).ConfigureAwait(false);
            if (!getResult.IsDefined(out var session))
                return Result<ILockedSession<TSession>>.FromError(getResult);

            return new LockedSession<TSession>(session, @lock);
        }
        catch (Exception ex)
        {
            if (@lock is not null)
                await @lock.DisposeAsync().ConfigureAwait(false);
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
            var lockResult = await _dataProvider.LockAsync<TSession>(key,
                    _cacheSettings.GetLockExpirationOrDefault<TSession>(), lockWaitTime, lockRetryTime, ct)
                .ConfigureAwait(false);
            if (!lockResult.IsDefined(out @lock))
                return Result<ILockedSession<TSession>>.FromError(lockResult);

            var getResult = await _dataProvider.GetAsync<TSession>(key, ct).ConfigureAwait(false);
            if (!getResult.IsDefined(out var session))
                return Result<ILockedSession<TSession>>.FromError(getResult);

            return new LockedSession<TSession>(session, @lock);
        }
        catch (Exception ex)
        {
            if (@lock is not null)
                await @lock.DisposeAsync().ConfigureAwait(false);
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
            return await _dataProvider.AddAsync(session, _cacheSettings.GetSessionEntryOptions<TSession>(), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RefreshAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await RefreshAsync<TSession>(session.Key, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result> RefreshAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.RefreshAsync<TSession>(key, ct).ConfigureAwait(false);
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
            return await _dataProvider.UpdateAsync(session, ct).ConfigureAwait(false);
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
            return await _dataProvider.UpdateAndGetAsync(session, ct).ConfigureAwait(false);
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
            return await _dataProvider
                .LockAsync<TSession>(key, lockExpirationTime ?? _cacheSettings.GetLockExpirationOrDefault<TSession>(),
                    ct).ConfigureAwait(false);
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
            return await _dataProvider.LockAsync<TSession>(key, lockExpirationTime, lockWaitTime, lockRetryTime, ct)
                .ConfigureAwait(false);
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
            return await _dataProvider.LockAsync<TSession>(key, _cacheSettings.GetLockExpirationOrDefault<TSession>(),
                lockWaitTime, lockRetryTime, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan? lockExpirationTime = null,
        CancellationToken ct = default) where TSession : Session
        => await LockAsync<TSession>(session.Key, lockExpirationTime, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan lockExpirationTime,
        TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
        => await LockAsync<TSession>(session.Key, lockExpirationTime, lockWaitTime, lockRetryTime, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<ISessionLock>> LockAsync<TSession>(TSession session, TimeSpan lockWaitTime,
        TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : Session
        => await LockAsync<TSession>(session.Key, lockWaitTime, lockRetryTime, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result> FinishAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await RefreshAsync<TSession>(session.Key, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result> FinishAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.EvictAsync<TSession>(key,
                _cacheSettings.GetEvictionAbsoluteExpirationOrDefault<TSession>() ??
                throw new InvalidOperationException(), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> FinishAndGetAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await FinishAndGetAsync<TSession>(session.Key, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<TSession>> FinishAndGetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.EvictAndGetAsync<TSession>(key,
                _cacheSettings.GetEvictionAbsoluteExpirationOrDefault<TSession>() ??
                throw new InvalidOperationException(), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ResumeAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await ResumeAsync<TSession>(session.Key, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result> ResumeAsync<TSession>(string key, CancellationToken ct = default) where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.RestoreAsync<TSession>(key,
                _cacheSettings.GetSessionEntryOptions<TSession>() ??
                throw new InvalidOperationException(), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TSession>> ResumeAndGetAsync<TSession>(TSession session, CancellationToken ct = default)
        where TSession : Session
        => await ResumeAndGetAsync<TSession>(session.Key, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<TSession>> ResumeAndGetAsync<TSession>(string key, CancellationToken ct = default)
        where TSession : Session
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            return await _dataProvider.RestoreAndGetAsync<TSession>(key,
                _cacheSettings.GetSessionEntryOptions<TSession>() ??
                throw new InvalidOperationException(), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }
}
