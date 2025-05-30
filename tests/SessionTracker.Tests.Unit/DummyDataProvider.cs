namespace SessionTracker.Tests.Unit;

public class DummyDataProvider : ISessionDataProvider
{
    public Task<Result<TSession>> GetAsync<TSession>(string key, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result<TSession>> GetEvictedAsync<TSession>(string key, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result> AddAsync<TSession>(TSession session, SessionEntryOptions options, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result> RefreshAsync<TSession>(string key, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result> UpdateAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result<TSession>> UpdateAndGetAsync<TSession>(TSession session, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result> EvictAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result<TSession>> EvictAndGetAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result> RestoreAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result<TSession>> RestoreAndGetAsync<TSession>(string key, SessionEntryOptions options, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }
}