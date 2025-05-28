namespace SessionTracker.Tests.Unit;

public class DummyLockProvider : ISessionLockProvider
{
    public Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, TimeSpan lockWaitTime, TimeSpan lockRetryTime,
        CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }

    public Task<Result<ISessionLock>> AcquireAsync<TSession>(string resource, TimeSpan lockExpirationTime, CancellationToken ct = default) where TSession : global::SessionTracker.Session
    {
        throw new NotImplementedException();
    }
}