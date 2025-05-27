using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace SessionTracker.Tests.Unit;

[UsedImplicitly]
public class SessionTrackerTestsFixture
{
    public readonly Mock<ISessionDataProvider> DataProviderMock = new();
    public readonly Mock<IOptions<SessionTrackerSettings>> SettingsMock = new();
    public readonly Mock<ISessionLockProvider> LockProviderMock = new();
    
    public Session Session => new(TestSessionKey);
    public string TestSessionKey => "test";
    public CancellationTokenSource Cts => new ();

    public SessionTrackerTestsFixture()
    {
        var opt = new SessionTrackerSettings();
        SettingsMock.Setup(x => x.Value).Returns(opt);
        Service = new(DataProviderMock.Object, LockProviderMock.Object, SettingsMock.Object);
    }

    public global::SessionTracker.SessionTracker Service { get; }

    public void Reset()
        => DataProviderMock.Reset();
}